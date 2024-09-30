using BattleGearUnpacker.Core.Exceptions;
using BattleGearUnpacker.Core.Graphics.Quantization;
using Hjg.Pngcs.Chunks;
using Hjg.Pngcs;
using System.Drawing;

namespace BattleGearUnpacker.Core.Graphics
{
    /// <summary>
    /// Image utilities mainly used for conversion between TIM2 and PNG.
    /// </summary>
    internal static class ImageUtil
    {
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
            var writer = new PngWriter(fs, info)
            {
                LeaveOpen = false
            };

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
                            line.ScanlineBytes[byteIndex] = (byte)((pixel1 << 4) | pixel2);
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
        /// Reads a PNG into an array of pixels, and a palette if necessary.<br/>
        /// 16-bit colors will be reduced into 8-bit either way.
        /// </summary>
        /// <param name="path">The path to the PNG to read.</param>
        /// <param name="image">The pixel data read.</param>
        /// <param name="palette">The palette read if present.</param>
        /// <param name="indexed">Whether or not the PNG was indexed.</param>
        /// <param name="bitDepth">How many bits there are per pixel in the PNG.</param>
        /// <exception cref="FriendlyException">The image dimensions were too large.</exception>
        /// <exception cref="Exception">Exclusive color types were detected being used together.</exception>
        public static void ReadPNG(string path, out Pixel[] image, out Color[] palette, out bool indexed, out int bitDepth)
        {
            using var fs = File.OpenRead(path);
            var reader = new PngReader(fs);
            reader.SetUnpackedMode(false);

            ImageInfo imageInfo = reader.ImgInfo;
            int width = imageInfo.Columns;
            int height = imageInfo.Rows;
            bitDepth = imageInfo.BitDepth;

            if (width > ushort.MaxValue || height > ushort.MaxValue)
                throw new FriendlyException($"Image dimensions too large: {width},{height}");

            image = new Pixel[width * height];
            bool trueColor = reader.ImgInfo.Channels >= 3;
            bool grayScale = reader.ImgInfo.Grayscale;
            indexed = reader.ImgInfo.Indexed;

            if ((trueColor && (grayScale || indexed))
                || (grayScale && (trueColor || indexed))
                || (indexed && (grayScale || trueColor)))
                throw new Exception($"True color, grayscale, and indexed are exclusive, please check reader or data.");

            palette = reader.GetPaletteColors();
            if (indexed)
            {
                for (int y = 0; y < height; y++)
                {
                    var indices = reader.ReadLineIndices(y);
                    for (int x = 0; x < width; x++)
                    {
                        int index = indices[x];
                        image[x + (y * width)] = new Pixel(palette[index], index);
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    var colors = reader.ReadLineColors(y);
                    for (int x = 0; x < width; x++)
                        image[x + (y * width)] = new Pixel(colors[x], -1);
                }
            }
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
        public static void ConvertPixelFormat(bool indexed, bool targetIndexed, int bitDepth, int targetBitDepth, bool createPalette, Pixel[] image, Color[] palette, out Pixel[] outImage, out Color[] outPalette)
        {
            if (indexed)
            {
                int paletteSize = 2 << (bitDepth - 1);
                int targetPaletteSize = 2 << (targetBitDepth - 1);
                if (targetPaletteSize > paletteSize)
                {
                    // Upgrade palette
                    Color[] newPalette = new Color[targetPaletteSize];
                    Array.Copy(palette, newPalette, palette.Length);
                    palette = newPalette;
                }
                else if (targetPaletteSize < paletteSize)
                {
                    // Quantize colors to smaller number of colors
                    // Already storing colors in Pixel object when reading palette initially
                    if (createPalette)
                    {
                        image = ColorPaletteQuantizer.Quantize(image, palette);
                    }
                    else
                    {
                        image = ColorMedianCutQuantizer.Quantize(image, targetPaletteSize, out palette);
                    }
                }
            }
            else if (targetIndexed)
            {
                // Quantize colors to palette
                if (createPalette)
                {
                    image = ColorPaletteQuantizer.Quantize(image, palette);
                }
                else
                {
                    int targetPaletteSize = 2 << (targetBitDepth - 1);
                    image = ColorMedianCutQuantizer.Quantize(image, targetPaletteSize, out palette);
                }
            }

            outImage = image;
            outPalette = palette;
        }
    }
}
