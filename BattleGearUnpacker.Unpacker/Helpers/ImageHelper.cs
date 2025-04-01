using BattleGearUnpacker.Core.Graphics;
using BattleGearUnpacker.Unpacker.Exceptions;
using Pngcs;
using Pngcs.Chunks;
using Pngcs.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Color = System.Drawing.Color;
using ImageInfo = Pngcs.ImageInfo;
using ISColor = SixLabors.ImageSharp.Color;

namespace BattleGearUnpacker.Unpacker.Helpers
{
    internal static class ImageHelper
    {
        /// <summary>
        /// Reads a PNG into an array of pixels, and a palette if necessary.<br/>
        /// 16-bit colors will be reduced into 8-bit either way.
        /// </summary>
        /// <param name="path">The path to the PNG to read.</param>
        /// <param name="maxWidth">The maximum width the image can be.</param>
        /// <param name="maxWidth">The maximum height the image can be.</param>
        /// <param name="image">The pixel data read.</param>
        /// <param name="palette">The palette read if present.</param>
        /// <param name="indexed">Whether or not the PNG was indexed.</param>
        /// <param name="bitDepth">How many bits there are per pixel in the PNG.</param>
        /// <param name="width">The actual read width of the image.</param>
        /// <param name="height">The actual read height of the image.</param>
        /// <exception cref="Exception">The image dimensions were too large.</exception>
        /// <exception cref="Exception">Exclusive color types were detected being used together.</exception>
        public static void ReadPNG(string path, int maxWidth, int maxHeight, out Pixel[] image, out Color[] palette, out bool indexed, out int bitDepth, out int width, out int height)
        {
            using var fs = File.OpenRead(path);
            var reader = new PngReader(fs);

            ImageInfo imageInfo = reader.ImgInfo;
            width = imageInfo.Columns;
            height = imageInfo.Rows;
            bitDepth = imageInfo.BitDepth;

            if (width > maxWidth || height > maxHeight)
                throw new FriendlyException($"Image dimensions too large: {width},{height} is above {maxWidth},{maxHeight}");

            image = new Pixel[width * height];
            bool trueColor = reader.ImgInfo.Channels >= 3;
            bool grayScale = reader.ImgInfo.Grayscale;
            indexed = reader.ImgInfo.Indexed;

            if (trueColor && (grayScale || indexed)
                || grayScale && (trueColor || indexed)
                || indexed && (grayScale || trueColor))
                throw new FriendlyException($"True color, grayscale, and indexed are exclusive, please check reader or data.");

            palette = reader.GetPaletteColors();
            if (indexed)
            {
                for (int y = 0; y < height; y++)
                {
                    var indices = reader.ReadLineIndices(y);
                    for (int x = 0; x < width; x++)
                    {
                        int index = indices[x];
                        image[x + y * width] = new Pixel(palette[index], index);
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    var colors = reader.ReadLineColors(y);
                    for (int x = 0; x < width; x++)
                    {
                        image[x + y * width] = new Pixel(colors[x], -1);
                    }
                }
            }
        }

        /// <summary>
        /// Converts image data into a PNG.
        /// </summary>
        /// <param name="outPath">The path to write the PNG to.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="bitDepth">The bitdepth per pixel.</param>
        /// <param name="hasAlpha">Whether or not the image data has an alpha channel or wishes to include it.</param>
        /// <param name="indexed">Whether or not the image data is indexed.</param>
        /// <param name="image">The image data to write to PNG.</param>
        /// <param name="palette">The palette for indexed image data, may be empty if not indexed.</param>
        public static void WritePNG(string outPath, int width, int height, int bitDepth, bool hasAlpha, bool indexed, Pixel[] image, Color[] palette)
        {
            using var fs = File.OpenWrite(outPath);
            var info = new ImageInfo(width, height, bitDepth, !indexed && hasAlpha, false, indexed);
            var writer = new PngWriter(fs, info);

            // Create palette
            if (indexed)
            {
                PngMetadata metadata = writer.GetMetadata();
                PngChunkPLTE plte = metadata.CreatePLTE();
                plte.SetLength(palette.Length);
                for (int clutIndex = 0; clutIndex < palette.Length; clutIndex++)
                {
                    plte.SetEntry(clutIndex, palette[clutIndex].R, palette[clutIndex].G, palette[clutIndex].B);
                }

                // Create transparency for palette
                if (hasAlpha)
                {
                    int[] clutAlphaValues = new int[palette.Length];
                    for (int clutIndex = 0; clutIndex < palette.Length; clutIndex++)
                    {
                        clutAlphaValues[clutIndex] = palette[clutIndex].A;
                    }

                    PngChunkTRNS transparency = metadata.CreateTRNS();
                    transparency.SetPaletteAlpha(clutAlphaValues);
                }
            }

            // Create image
            bool index4 = bitDepth == 4;
            int byteCount = index4 ? width / 2 : width;
            int channelWidth = hasAlpha ? width * 4 : width * 3;
            int pixelIndex = 0;
            ImageLine.SampleType sampleType = indexed ? ImageLine.SampleType.Byte : ImageLine.SampleType.Integer;
            if (sampleType == ImageLine.SampleType.Integer)
            {
                for (int lineIndex = 0; lineIndex < height; lineIndex++)
                {
                    var line = new ImageLine(info, sampleType);
                    for (int i = 0; i < channelWidth;)
                    {
                        Color color = image[pixelIndex++].Color;
                        if (hasAlpha)
                        {
                            line.ScanlineInts[i++] = color.R;
                            line.ScanlineInts[i++] = color.G;
                            line.ScanlineInts[i++] = color.B;
                            line.ScanlineInts[i++] = color.A;
                        }
                        else
                        {
                            line.ScanlineInts[i++] = color.R;
                            line.ScanlineInts[i++] = color.G;
                            line.ScanlineInts[i++] = color.B;
                        }
                    }

                    writer.WriteRow(line, lineIndex);
                }
            }
            else if (sampleType == ImageLine.SampleType.Byte)
            {
                for (int lineIndex = 0; lineIndex < height; lineIndex++)
                {
                    var line = new ImageLine(info, sampleType);
                    for (int byteIndex = 0; byteIndex < byteCount; byteIndex++)
                    {
                        if (index4)
                        {
                            byte pixel1 = (byte)image[pixelIndex++].Index;
                            byte pixel2 = (byte)image[pixelIndex++].Index;
                            line.ScanlineBytes[byteIndex] = (byte)(pixel1 << 4 | pixel2);
                        }
                        else
                        {
                            line.ScanlineBytes[byteIndex] = (byte)image[pixelIndex++].Index;
                        }
                    }

                    writer.WriteRow(line, lineIndex);
                }
            }

            writer.End();
            fs.Dispose();
        }

        /// <summary>
        /// Converts pixels from true color to indexed if necessary.<br/>
        /// For indexed images the output palette will be the fully supported size of the target bit depth.
        /// </summary>
        /// <param name="indexed">Whether or not the source is indexed.</param>
        /// <param name="targetIndexed">Whether or not the target is indexed.</param>
        /// <param name="bitDepth">The bitdepth per pixel of the source.</param>
        /// <param name="targetBitDepth">The bitdepth per pixel of the target.</param>
        /// <param name="createPalette">Whether to create the palette or use the existing one.</param>
        /// <param name="image">The source image data.</param>
        /// <param name="palette">The source palette, may be empty.</param>
        /// <param name="outImage">The output image data.</param>
        /// <param name="outPalette">The output palette, may be empty.</param>
        public static void NormalizePixelFormat(int width, int height, int bitDepth, int targetBitDepth, bool indexed, bool targetIndexed, bool createPalette, Pixel[] image, Color[] palette, out Pixel[] outImage, out Color[] outPalette)
        {
            if (targetIndexed)
            {
                int paletteSize = 2 << bitDepth - 1;
                int targetPaletteSize = 2 << targetBitDepth - 1;

                bool upgradePalette = indexed && targetPaletteSize > paletteSize;
                bool downSizePalette = indexed && targetPaletteSize < paletteSize;
                bool newPalette = !indexed || targetPaletteSize != paletteSize;

                if (upgradePalette)
                {
                    var tempPalette = new Color[targetPaletteSize];
                    Array.Copy(palette, tempPalette, paletteSize);
                    palette = tempPalette;
                }
                else if (downSizePalette)
                {
                    QuantizeExistingLargerPalette(image, palette, targetBitDepth, width, height, out image, out palette);
                }
                else if (newPalette)
                {
                    QuantizeNewPalette(image, targetBitDepth, width, height, out image, out palette);
                }
            }

            outImage = image;
            outPalette = palette;
        }

        private static void QuantizeNewPalette(Pixel[] image, int bitDepth, int width, int height, out Pixel[] outImage, out Color[] outPalette)
        {
            int paletteSize = 2 << bitDepth - 1;
            QuantizerOptions options = new QuantizerOptions
            {
                MaxColors = paletteSize
            };

            var quantizer = new WuQuantizer(options);
            var pixelQuantizer = quantizer.CreatePixelSpecificQuantizer<Rgba32>(Configuration.Default);
            var newImage = ToImageSharpImage(image, width, height).Frames.RootFrame;
            var newBounds = new Rectangle(0, 0, width, height);
            var indexedImage = pixelQuantizer.BuildPaletteAndQuantizeFrame(newImage, newBounds);
            ToTimImage(indexedImage, paletteSize, out outImage, out outPalette);
        }

        private static void QuantizeExistingPalette(Pixel[] image, Color[] palette, int width, int height, out Pixel[] outImage)
        {
            int paletteSize = palette.Length;
            QuantizerOptions options = new QuantizerOptions
            {
                MaxColors = paletteSize
            };

            var quantizer = new PaletteQuantizer(ToImageSharpColor(palette), options);
            var pixelQuantizer = quantizer.CreatePixelSpecificQuantizer<Rgba32>(Configuration.Default);
            var newImage = ToImageSharpImage(image, width, height).Frames.RootFrame;
            var newBounds = new Rectangle(0, 0, width, height);
            var newIndexedImage = pixelQuantizer.QuantizeFrame(newImage, newBounds);
            ToTimImage(newIndexedImage, paletteSize, out outImage, out Color[] newPalette);

            Debug.Assert(newPalette.Length == palette.Length);
            for (int i = 0; i < paletteSize; i++)
            {
                Debug.Assert(newPalette[i] == palette[i]);
            }
        }

        private static void QuantizeExistingLargerPalette(Pixel[] image, Color[] palette, int bitDepth, int width, int height, out Pixel[] outImage, out Color[] outPalette)
        {
            int paletteSize = 2 << bitDepth - 1;
            QuantizerOptions options = new QuantizerOptions
            {
                MaxColors = paletteSize
            };

            var quantizer = new PaletteQuantizer(ToImageSharpColor(palette), options);
            var pixelQuantizer = quantizer.CreatePixelSpecificQuantizer<Rgba32>(Configuration.Default);
            var newImage = ToImageSharpImage(image, width, height).Frames.RootFrame;
            var newBounds = new Rectangle(0, 0, width, height);
            var newIndexedImage = pixelQuantizer.BuildPaletteAndQuantizeFrame(newImage, newBounds);
            ToTimImage(newIndexedImage, paletteSize, out outImage, out outPalette);
        }

        private static void ToTimImage(IndexedImageFrame<Rgba32> indexedImage, int paletteSize, out Pixel[] outImage, out Color[] outPalette)
        {
            int width = indexedImage.Width;
            int height = indexedImage.Height;
            var palette = indexedImage.Palette;
            outImage = new Pixel[width * height];
            outPalette = new Color[paletteSize];

            var paletteSpan = palette.Span;
            for (int i = 0; i < palette.Length; i++)
            {
                outPalette[i] = ToDrawingColor(paletteSpan[i]);
            }

            for (int i = palette.Length; i < paletteSize; i++)
            {
                outPalette[i] = Color.FromArgb(0, 0, 0, 0);
            }

            for (int y = 0; y < height; y++)
            {
                var row = indexedImage.DangerousGetRowSpan(y);

                for (int x = 0; x < width; x++)
                {
                    byte index = row[x];
                    outImage[y * width + x] = new Pixel(outPalette[index], index);
                }
            }
        }

        private static Image<Rgba32> ToImageSharpImage(Pixel[] image, int width, int height)
        {
            var newImage = new Image<Rgba32>(Configuration.Default, width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    newImage[x, y] = ToImageSharpRgba32(image[y * width + x].Color);
                }
            }
            return newImage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Rgba32 ToImageSharpRgba32(Color color)
            => new Rgba32(color.R, color.G, color.B, color.A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color ToDrawingColor(Rgba32 color)
            => Color.FromArgb(color.A, color.R, color.G, color.B);

        private static ISColor[] ToImageSharpColor(Color[] colors)
        {
            var result = new ISColor[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                result[i] = ISColor.FromRgba(color.R, color.G, color.B, color.A);
            }
            return result;
        }
    }
}
