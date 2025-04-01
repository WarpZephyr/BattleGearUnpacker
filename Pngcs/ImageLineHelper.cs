using Pngcs.Chunks;
using System;

namespace Pngcs
{
    /// <summary>
    /// Bunch of utility static methods to process/analyze an image line.<br/>
    /// Not essential at all, some methods are probably to be removed if future releases.<br/>
    /// TODO: document this better.
    /// </summary>
    public static class ImageLineHelper
    {
        /// <summary>
        /// Given an indexed line with a palette, unpacks as a RGB array
        /// </summary>
        /// <param name="line">ImageLine as returned from PngReader</param>
        /// <param name="pal">Palette chunk</param>
        /// <param name="trns">TRNS chunk (optional)</param>
        /// <param name="buf">Preallocated array, optional</param>
        /// <returns>R G B (one byte per sample)</returns>
        public static int[] PaletteToRGB(ImageLine line, PngChunkPLTE pal, PngChunkTRNS? trns, int[] buf)
        {
            int channels = trns != null ? 4 : 3;
            int nsamples = line.ImgInfo.Columns * channels;
            if (buf == null || buf.Length < nsamples)
                buf = new int[nsamples];
            if (!line.SamplesUnpacked)
                line = line.UnpackToNewImageLine();
            bool isbyte = line.LineSampleType == ImageLine.SampleType.Byte;
            
            if (trns != null)
            {
                var paletteAlpha = trns.GetPaletteAlpha();
                int nindexesWithAlpha = paletteAlpha.Length;
                for (int c = 0; c < line.ImgInfo.Columns; c++)
                {
                    int index = isbyte ? (line.ScanlineBytes[c] & 0xFF) : line.ScanlineInts[c];
                    pal.GetEntryRgb(index, buf, c * channels);
                    int alpha = index < nindexesWithAlpha ? paletteAlpha[index] : 255;
                    buf[c * channels + 3] = alpha;
                }
            }
            else
            {
                for (int c = 0; c < line.ImgInfo.Columns; c++)
                {
                    int index = isbyte ? (line.ScanlineBytes[c] & 0xFF) : line.ScanlineInts[c];
                    pal.GetEntryRgb(index, buf, c * channels);
                }
            }
            
            return buf;
        }

        public static int[] PaletteToRGB(ImageLine line, PngChunkPLTE pal, int[] buf)
            => PaletteToRGB(line, pal, null, buf);

        public static int ToARGB8(int r, int g, int b)
            => unchecked(((int)0xFF000000) | ((r) << 16) | ((g) << 8) | (b));

        public static int ToARGB8(int r, int g, int b, int a)
            => ((a) << 24) | ((r) << 16) | ((g) << 8) | (b);

        public static int ToARGB8(int[] buff, int offset, bool alpha)
            => alpha ? ToARGB8(buff[offset++], buff[offset++], buff[offset++], buff[offset])
            : ToARGB8(buff[offset++], buff[offset++], buff[offset]);

        public static int ToARGB8(byte[] buff, int offset, bool alpha)
            => alpha ? ToARGB8(buff[offset++], buff[offset++], buff[offset++], buff[offset])
            : ToARGB8(buff[offset++], buff[offset++], buff[offset]);

        public static void FromARGB8(int val, int[] buff, int offset, bool alpha)
        {
            buff[offset++] = (val >> 16) & 0xFF;
            buff[offset++] = (val >> 8) & 0xFF;
            buff[offset] = val & 0xFF;
            if (alpha)
                buff[offset + 1] = (val >> 24) & 0xFF;
        }

        public static void FromARGB8(int val, byte[] buff, int offset, bool alpha)
        {
            buff[offset++] = (byte)((val >> 16) & 0xFF);
            buff[offset++] = (byte)((val >> 8) & 0xFF);
            buff[offset] = (byte)(val & 0xFF);
            if (alpha)
                buff[offset + 1] = (byte)((val >> 24) & 0xFF);
        }

        public static int GetPixelToARGB8(ImageLine line, int column)
        {
            if (line.IsInt())
                return ToARGB8(line.ScanlineInts, column * line.channels, line.ImgInfo.HasAlpha);
            else
                return ToARGB8(line.ScanlineBytes, column * line.channels, line.ImgInfo.HasAlpha);
        }

        public static void SetPixelFromARGB8(ImageLine line, int column, int argb)
        {
            if (line.IsInt())
                FromARGB8(argb, line.ScanlineInts, column * line.channels, line.ImgInfo.HasAlpha);
            else
                FromARGB8(argb, line.ScanlineBytes, column * line.channels, line.ImgInfo.HasAlpha);
        }

        public static void SetPixel(ImageLine line, int col, int r, int g, int b, int a)
        {
            int offset = col * line.channels;
            if (line.IsInt())
            {
                line.ScanlineInts[offset++] = r;
                line.ScanlineInts[offset++] = g;
                line.ScanlineInts[offset] = b;
                if (line.ImgInfo.HasAlpha)
                    line.ScanlineInts[offset + 1] = a;
            }
            else
            {
                line.ScanlineBytes[offset++] = (byte)r;
                line.ScanlineBytes[offset++] = (byte)g;
                line.ScanlineBytes[offset] = (byte)b;
                if (line.ImgInfo.HasAlpha)
                    line.ScanlineBytes[offset + 1] = (byte)a;
            }
        }

        public static void SetPixel(ImageLine line, int col, int r, int g, int b)
            => SetPixel(line, col, r, g, b, line.MaxSampleVal);

        public static double ReadDouble(ImageLine line, int pos)
        {
            if (line.IsInt())
                return line.ScanlineInts[pos] / (line.MaxSampleVal + 0.9);
            else
                return line.ScanlineBytes[pos] / (line.MaxSampleVal + 0.9);
        }

        public static void WriteDouble(ImageLine line, double d, int pos)
        {
            if (line.IsInt())
                line.ScanlineInts[pos] = (int)(d * (line.MaxSampleVal + 0.99));
            else
                line.ScanlineBytes[pos] = (byte)(d * (line.MaxSampleVal + 0.99));
        }

        public static int Interpol(int a, int b, int c, int d, double dx, double dy)
        {
            // a b -> x (0-1)
            // c d
            double e = a * (1.0 - dx) + b * dx;
            double f = c * (1.0 - dx) + d * dx;
            return (int)(e * (1 - dy) + f * dy + 0.5);
        }

        public static int ClampTo_0_255(int i)
            => i > 255 ? 255 : (i < 0 ? 0 : i);

        /// <summary>
        /// [0,1)
        /// </summary>
        public static double ClampDouble(double i)
            => i < 0 ? 0 : (i >= 1 ? 0.999999 : i);

        public static int ClampTo_0_65535(int i)
            => i > 65535 ? 65535 : (i < 0 ? 0 : i);

        public static int ClampTo_128_127(int x)
            => x > 127 ? 127 : (x < -128 ? -128 : x);

        public static int[] Unpack(ImageInfo imageInfo, int[] source, int[] destination, bool scale)
        {
            int len1 = imageInfo.SamplesPerRow;
            int len0 = imageInfo.SamplesPerRowPacked;
            if (destination == null || destination.Length < len1)
                destination = new int[len1];
            if (imageInfo.Packed)
                ImageLine.UnpackInplaceInts(imageInfo, source, destination, scale);
            else
                Array.Copy(source, 0, destination, 0, len0);
            return destination;
        }

        public static byte[] Unpack(ImageInfo imageInfo, byte[] source, byte[] destination, bool scale)
        {
            int length = imageInfo.SamplesPerRow;
            int lengthPacked = imageInfo.SamplesPerRowPacked;
            if (destination == null || destination.Length < length)
                destination = new byte[length];
            if (imageInfo.Packed)
                ImageLine.UnpackInplaceBytes(imageInfo, source, destination, scale);
            else
                Array.Copy(source, 0, destination, 0, lengthPacked);

            return destination;
        }

        public static int[] Pack(ImageInfo imageInfo, int[] source, int[] destination, bool scale)
        {
            int lengthPacked = imageInfo.SamplesPerRowPacked;
            if (destination == null || destination.Length < lengthPacked)
                destination = new int[lengthPacked];
            if (imageInfo.Packed)
                ImageLine.PackInplaceInts(imageInfo, source, destination, scale);
            else
                Array.Copy(source, 0, destination, 0, lengthPacked);

            return destination;
        }

        public static byte[] Pack(ImageInfo imageInfo, byte[] source, byte[] destination, bool scale)
        {
            int len0 = imageInfo.SamplesPerRowPacked;
            if (destination == null || destination.Length < len0)
                destination = new byte[len0];
            if (imageInfo.Packed)
                ImageLine.PackInplaceBytes(imageInfo, source, destination, scale);
            else
                Array.Copy(source, 0, destination, 0, len0);

            return destination;
        }
    }
}
