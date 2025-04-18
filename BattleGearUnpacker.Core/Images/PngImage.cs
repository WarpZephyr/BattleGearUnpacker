using BattleGearUnpacker.Core.Graphics;
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
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace BattleGearUnpacker.Core.Images
{
    public class PngImage
    {
        public int Width { get; init; }
        public int Height { get; init; }

        private int BitDepthField;
        public int BitDepth
        {
            get => BitDepthField;
            set
            {
                if (IsIndexed && BitDepthField != value)
                {
                    if (value > 8)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(BitDepth)} should be {8} or less.");
                    }

                    InvalidateCache = true;
                }

                BitDepthField = value;
            }
        }

        private bool HasAlphaField;
        public bool HasAlpha
        {
            get => HasAlphaField;
            set
            {
                // Normalize alpha
                if (!HasAlphaField && value)
                {
                    for (int i = 0; i < Length; i++)
                    {
                        Image[i].Color = Color.FromArgb(0xFF, Image[i].Color);
                    }
                }

                HasAlphaField = value;
            }
        }

        private bool IsIndexedField;
        public bool IsIndexed
        {
            get => IsIndexedField;
            set
            {
                if (!IsIndexedField && value)
                {
                    InvalidateCache |= true;
                }

                IsIndexedField = value;
            }
        }

        private Color[] PaletteField;
        public Color[] Palette
        {
            get => PaletteField;
            set
            {
                if (PaletteField.Length != value.Length)
                {
                    InvalidateCache = true;
                }

                PaletteField = value;
            }
        }

        public Pixel[] Image { get; init; }
        private bool InvalidateCache;
        public int Length => Width * Height;

        public PngImage(int width, int height, int bitDepth, bool hasAlpha, bool isIndexed, Color[] palette, Pixel[] image)
        {
            if (image.Length < (width * height))
            {
                throw new ArgumentException($"{nameof(image)} has an invalid length for the provided {nameof(width)} and {nameof(height)}: {image.Length} < ({width} * {height})", nameof(image));
            }

            Width = width;
            Height = height;
            BitDepthField = bitDepth;
            HasAlphaField = hasAlpha;
            IsIndexedField = isIndexed;
            PaletteField = palette;
            Image = image;
            InvalidateCache = false;
        }

        #region Read

        public static PngImage Read(string path)
        {
            using var fs = File.OpenRead(path);
            var reader = new PngReader(fs);

            ImageInfo imageInfo = reader.ImgInfo;
            int width = imageInfo.Columns;
            int height = imageInfo.Rows;
            int bitDepth = imageInfo.BitDepth;

            var image = new Pixel[width * height];
            bool trueColor = reader.ImgInfo.Channels >= 3;
            bool grayScale = reader.ImgInfo.Grayscale;
            bool indexed = reader.ImgInfo.Indexed;

            if (trueColor && (grayScale || indexed)
                || grayScale && (trueColor || indexed)
                || indexed && (grayScale || trueColor))
                throw new InvalidDataException($"True color, grayscale, and indexed are exclusive, please check reader or data.");

            var palette = reader.GetPaletteColors();
            ReadInto(reader, image, palette, indexed, width, height);
            return new PngImage(width, height, Math.Min(bitDepth, 8), reader.ImgInfo.HasAlpha, indexed, palette, image);
        }

        public void ReadSubImage(string path, int x, int y)
        {
            using var fs = File.OpenRead(path);
            var reader = new PngReader(fs);

            ImageInfo imageInfo = reader.ImgInfo;
            int width = imageInfo.Columns;
            int height = imageInfo.Rows;
            int bitDepth = imageInfo.BitDepth;

            bool trueColor = reader.ImgInfo.Channels >= 3;
            bool grayScale = reader.ImgInfo.Grayscale;
            bool indexed = reader.ImgInfo.Indexed;
            bool hasAlpha = reader.ImgInfo.HasAlpha;

            if (trueColor && (grayScale || indexed)
                || grayScale && (trueColor || indexed)
                || indexed && (grayScale || trueColor))
                throw new InvalidDataException($"True color, grayscale, and indexed are exclusive, please check reader or data.");

            int offset = x + (y * Width);
            int length = width * height;
            var span = Image.AsSpan(offset, length);
            var palette = reader.GetPaletteColors();
            ReadInto(reader, span, palette, indexed, width, height);

            // Normalize Alpha
            if (HasAlpha && !hasAlpha)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Color = Color.FromArgb(0xFF, span[i].Color);
                }
            }

            CheckCache(palette, indexed);
        }

        private static void ReadInto(PngReader reader, Span<Pixel> image, Color[] palette, bool indexed, int width, int height)
        {
            if (indexed)
            {
                for (int y = 0; y < height; y++)
                {
                    var indices = reader.ReadLineIndices(y);
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int index = indices[x];
                        image[rowOffset + x] = new Pixel(palette[index], index);
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    var colors = reader.ReadLineColors(y);
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        image[rowOffset + x] = new Pixel(colors[x], -1);
                    }
                }
            }
        }

        #endregion

        #region Write

        public void Write(string path)
        {
            using var fs = File.OpenWrite(path);
            var info = new ImageInfo(Width, Height, BitDepth, !IsIndexed && HasAlpha, false, IsIndexed);
            var writer = new PngWriter(fs, info);

            // Create palette
            if (IsIndexed)
            {
                PngMetadata metadata = writer.GetMetadata();
                PngChunkPLTE plte = metadata.CreatePLTE();
                plte.SetLength(Palette.Length);
                for (int clutIndex = 0; clutIndex < Palette.Length; clutIndex++)
                {
                    plte.SetEntry(clutIndex, Palette[clutIndex].R, Palette[clutIndex].G, Palette[clutIndex].B);
                }

                // Create transparency for palette
                if (HasAlpha)
                {
                    int[] clutAlphaValues = new int[Palette.Length];
                    for (int clutIndex = 0; clutIndex < Palette.Length; clutIndex++)
                    {
                        clutAlphaValues[clutIndex] = Palette[clutIndex].A;
                    }

                    PngChunkTRNS transparency = metadata.CreateTRNS();
                    transparency.SetPaletteAlpha(clutAlphaValues);
                }
            }

            // Create image
            bool index4 = BitDepth == 4;
            int byteCount = index4 ? Width / 2 : Width;
            int channelWidth = HasAlpha ? Width * 4 : Width * 3;
            int pixelIndex = 0;
            ImageLine.SampleType sampleType = IsIndexed ? ImageLine.SampleType.Byte : ImageLine.SampleType.Integer;
            if (sampleType == ImageLine.SampleType.Integer)
            {
                for (int lineIndex = 0; lineIndex < Height; lineIndex++)
                {
                    var line = new ImageLine(info, sampleType);
                    for (int i = 0; i < channelWidth;)
                    {
                        Color color = Image[pixelIndex++].Color;
                        if (HasAlpha)
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
                for (int lineIndex = 0; lineIndex < Height; lineIndex++)
                {
                    var line = new ImageLine(info, sampleType);
                    for (int byteIndex = 0; byteIndex < byteCount; byteIndex++)
                    {
                        if (index4)
                        {
                            byte pixel1 = (byte)Image[pixelIndex++].Index;
                            byte pixel2 = (byte)Image[pixelIndex++].Index;
                            line.ScanlineBytes[byteIndex] = (byte)(pixel1 << 4 | pixel2);
                        }
                        else
                        {
                            line.ScanlineBytes[byteIndex] = (byte)Image[pixelIndex++].Index;
                        }
                    }

                    writer.WriteRow(line, lineIndex);
                }
            }

            writer.End();
            fs.Dispose();
        }

        #endregion

        #region Info Check

        private bool PaletteMatches(Color[] palette)
        {
            if (!IsIndexed)
            {
                return false;
            }

            if (palette.Length > Palette.Length)
            {
                return false;
            }

            for (int i = 0; i < palette.Length; i++)
            {
                if (Palette[i] != palette[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void CheckCache(Color[] otherPalette, bool otherIndexed)
        {
            if (InvalidateCache)
            {
                return;
            }

            if (IsIndexed && !otherIndexed)
            {
                InvalidateCache |= true;
                return;
            }

            InvalidateCache |= !PaletteMatches(otherPalette);
        }

        #endregion

        #region Sub Image

        public void Merge(PngImage other, int x, int y)
        {
            int remaining = x + (y * Width);
            if (remaining < 1)
            {
                throw new IndexOutOfRangeException($"This image does not have the size to use the specified coordinates: {x},{y}");
            }

            int subLength = other.Length;
            if (subLength > remaining)
            {
                throw new ArgumentOutOfRangeException(nameof(other), $"This image does not have the remaining size to contain the specified sub-image {nameof(other)} at the specified coordinates: {x},{y}");
            }

            int offset = remaining;
            Array.Copy(other.Image, 0, Image, offset, subLength);

            // Normalize Alpha
            if (HasAlpha && !other.HasAlpha)
            {
                var span = Image.AsSpan(offset, subLength);
                for (int i = 0; i < span.Length; i++)
                {
                    span[i].Color = Color.FromArgb(0xFF, span[i].Color);
                }
            }

            CheckCache(other.Palette, other.IsIndexed);
        }

        #endregion

        #region Normalization

        public void Normalize()
        {
            if (!InvalidateCache)
            {
                return;
            }

            QuantizeNewPalette(Image, BitDepth, Width, Height, out Color[] newPalette);
            Palette = newPalette;
            InvalidateCache = false;
        }

        public void NormalizeToPalette(Color[] palette)
        {
            if (!IsIndexed)
            {
                return;
            }

            if (InvalidateCache)
            {
                Normalize();
            }

            Palette = palette;
            QuantizeExistingPalette(Image, palette, Width, Height);
            InvalidateCache = false;
        }

        #endregion

        #region Quantization

        private static void QuantizeNewPalette(Pixel[] image, int bitDepth, int width, int height, out Color[] outPalette)
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
            ToTimImage(indexedImage, paletteSize, image, out outPalette);
        }

        private static void QuantizeExistingPalette(Pixel[] image, Color[] palette, int width, int height)
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
            ToTimImage(newIndexedImage, paletteSize, image, out Color[] newPalette);

            Debug.Assert(newPalette.Length == palette.Length);
            for (int i = 0; i < paletteSize; i++)
            {
                Debug.Assert(newPalette[i] == palette[i]);
            }
        }

        private static void ToTimImage(IndexedImageFrame<Rgba32> indexedImage, int paletteSize, Pixel[] outImage, out Color[] outPalette)
        {
            int width = indexedImage.Width;
            int height = indexedImage.Height;
            var palette = indexedImage.Palette;
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

        #endregion
    }
}
