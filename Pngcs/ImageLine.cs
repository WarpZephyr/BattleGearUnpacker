using Pngcs.Drawing;
using Pngcs.IO;
using System;

namespace Pngcs
{
    /// <summary>
    /// Lightweight wrapper for an image scanline, for read and write.
    /// </summary>
    /// <remarks>It can be (usually it is) reused while iterating over the image lines.<br/>
    /// See <see cref="ScanlineInts"/> field doc, to understand the format.
    ///</remarks>
    public class ImageLine
    {
        /// <summary>
        /// The sample type of an <see cref="ImageLine"/>.
        /// </summary>
        public enum SampleType
        {
            /// <summary>
            /// 4 bytes per sample.
            /// </summary>
            Integer,

            /// <summary>
            /// 1 byte per sample.
            /// </summary>
            Byte
        }

        /// <summary>
        /// ImageInfo (readonly inmutable)
        /// </summary>
        public ImageInfo ImgInfo { get; private set; }

        /// <summary>
        /// Samples of an image line
        /// </summary>
        /// <remarks>
        /// 
        /// The 'scanline' is an array of integers, corresponds to an image line (row)
        /// Except for 'packed' formats (gray/indexed with 1-2-4 bitdepth) each int is a
        /// "sample" (one for channel), (0-255 or 0-65535) in the respective PNG sequence
        /// sequence : (R G B R G B...) or (R G B A R G B A...) or (g g g ...) or ( i i i
        /// ) (palette index)
        /// 
        /// For bitdepth 1/2/4 ,and if samplesUnpacked=false, each value is a PACKED byte! To get an unpacked copy,
        /// see <c>Pack()</c> and its inverse <c>Unpack()</c>
        /// 
        /// To convert a indexed line to RGB balues, see ImageLineHelper.PalIdx2RGB()
        /// (cant do the reverse)
        /// </remarks>
        public int[] ScanlineInts { get; private set; }

        /// <summary>
        /// Same as Scanline, but with one byte per sample. Only one of ScanlineInts and ScanlineBytes is valid - this depends
        /// on SampleType}
        /// </summary>
        public byte[] ScanlineBytes { get; private set; }

        /// <summary>
        /// tracks the current row number (from 0 to rows-1)
        /// </summary>
        public int RowNum { get; set; }

        internal readonly int channels; // copied from imgInfo, more handy
        internal readonly int bitDepth; // copied from imgInfo, more handy

        /// <summary>
        /// Hown many elements has the scanline array
        /// =imgInfo.samplePerRowPacked, if packed, imgInfo.samplePerRow elsewhere
        /// </summary>
        public int ElementsPerRow { get; private set; }

        /// <summary>
        /// Maximum sample value that this line admits: typically 255; less if bitdepth less than 8, 65535 if 16bits
        /// </summary>
        public int MaxSampleVal { get; private set; }

        /// <summary>
        /// Determines if samples are stored in integers or in bytes
        /// </summary>
        public SampleType LineSampleType { get; private set; }

        /// <summary>
        /// True: each scanline element is a sample.
        /// False: each scanline element has severals samples packed in a byte
        /// </summary>
        public bool SamplesUnpacked { get; private set; }

        /// <summary>
        /// informational only ; filled by the reader
        /// </summary>
        public FilterType FilterUsed { get; set; }

        public ImageLine(ImageInfo imageInfo)
            : this(imageInfo, SampleType.Integer, false) { }

        public ImageLine(ImageInfo imageInfo, SampleType sampleType)
            : this(imageInfo, sampleType, false) { }

        /// <summary>
        /// Creates a new <see cref="ImageLine"/>.
        /// </summary>
        /// <param name="imageInfo">Immutable copy of PNG <see cref="ImageInfo"/>.</param>
        /// <param name="sampleType">Storage for samples:INT (default) or BYTE</param>
        /// <param name="unpackedMode">If true and bitdepth less than 8, samples are unpacked. This has no effect if biddepth 8 or 16.</param>
        public ImageLine(ImageInfo imageInfo, SampleType sampleType, bool unpackedMode)
            : this(imageInfo, sampleType, unpackedMode, null, null) { }

        internal ImageLine(ImageInfo imageInfo, SampleType sampleType, bool unpackedMode, int[]? sci, byte[]? scb)
        {
            ImgInfo = imageInfo;
            channels = imageInfo.Channels;
            bitDepth = imageInfo.BitDepth;
            FilterUsed = FilterType.FILTER_UNKNOWN;
            LineSampleType = sampleType;
            SamplesUnpacked = unpackedMode || !imageInfo.Packed;
            ElementsPerRow = SamplesUnpacked ? imageInfo.SamplesPerRow : imageInfo.SamplesPerRowPacked;
            if (sampleType == SampleType.Integer)
            {
                ScanlineInts = sci ?? (new int[ElementsPerRow]);
                ScanlineBytes = [];
                MaxSampleVal = bitDepth == 16 ? 0xFFFF : GetMaskForPackedFormatsLs(bitDepth);
            }
            else if (sampleType == SampleType.Byte)
            {
                ScanlineBytes = scb ?? (new byte[ElementsPerRow]);
                ScanlineInts = [];
                MaxSampleVal = bitDepth == 16 ? 0xFF : GetMaskForPackedFormatsLs(bitDepth);
            }
            else
            {
                throw new InvalidOperationException($"Unknown {nameof(SampleType)}: {sampleType}");
            }

            RowNum = -1;
        }

        #region Mask

        internal static int GetMaskForPackedFormats(int bitDepth)
        {
            // Utility function for pack/unpack
            if (bitDepth == 4)
                return 0xf0;
            else if (bitDepth == 2)
                return 0xc0;
            else if (bitDepth == 1)
                return 0x80;

            return 0xff;
        }

        internal static int GetMaskForPackedFormatsLs(int bitDepth)
        {
            // Utility function for pack/unpack
            if (bitDepth == 4)
                return 0x0f;
            else if (bitDepth == 2)
                return 0x03;
            else if (bitDepth == 1)
                return 0x01;

            return 0xff;
        }

        #endregion

        #region Pack

        internal static void UnpackInplaceInts(ImageInfo iminfo, int[] src, int[] dst, bool Scale)
        {
            int bitDepth = iminfo.BitDepth;
            if (bitDepth >= 8)
                return; // nothing to do

            int mask = GetMaskForPackedFormatsLs(bitDepth);
            int scalefactor = 8 - bitDepth;
            int shift = 8 * iminfo.SamplesPerRowPacked - bitDepth * iminfo.SamplesPerRow;

            int shiftedMask;
            int unshift;
            if (shift != 8)
            {
                shiftedMask = mask << shift;
                unshift = shift; // how many bits to shift the mask to the right to recover shiftedMask
            }
            else
            {
                shiftedMask = mask;
                unshift = 0;
            }

            for (int j = iminfo.SamplesPerRow - 1, i = iminfo.SamplesPerRowPacked - 1; j >= 0; j--)
            {
                int v = (src[i] & shiftedMask) >> unshift;
                if (Scale)
                    v <<= scalefactor;

                dst[j] = v;
                shiftedMask <<= bitDepth;
                unshift += bitDepth;
                if (unshift == 8)
                {
                    shiftedMask = mask;
                    unshift = 0;
                    i--;
                }
            }
        }

        internal static void UnpackInplaceBytes(ImageInfo iminfo, byte[] source, byte[] destination, bool scale)
        {
            int bitDepth = iminfo.BitDepth;
            if (bitDepth >= 8)
                return; // nothing to do

            int mask = GetMaskForPackedFormatsLs(bitDepth);
            int scalefactor = 8 - bitDepth;
            int shift = 8 * iminfo.SamplesPerRowPacked - bitDepth * iminfo.SamplesPerRow;

            int shiftedMask;
            int unshift;
            if (shift != 8)
            {
                shiftedMask = mask << shift;
                unshift = shift; // how many bits to shift the mask to the right to recover shiftedMask
            }
            else
            {
                shiftedMask = mask;
                unshift = 0;
            }

            for (int j = iminfo.SamplesPerRow - 1, i = iminfo.SamplesPerRowPacked - 1; j >= 0; j--)
            {
                int v = (source[i] & shiftedMask) >> unshift;
                if (scale)
                    v <<= scalefactor;

                destination[j] = (byte)v;
                shiftedMask <<= bitDepth;
                unshift += bitDepth;
                if (unshift == 8)
                {
                    shiftedMask = mask;
                    unshift = 0;
                    i--;
                }
            }
        }

        internal static void PackInplaceInts(ImageInfo iminfo, int[] src, int[] dst, bool scaled)
        {
            int bitDepth = iminfo.BitDepth;
            if (bitDepth >= 8)
                return; // nothing to do

            int mask0 = GetMaskForPackedFormatsLs(bitDepth);
            int scalefactor = 8 - bitDepth;
            int offset0 = 8 - bitDepth;
            int v;
            int v0;
            int offset = 8 - bitDepth;
            v0 = src[0]; // first value is special for in place
            dst[0] = 0;
            if (scaled)
                v0 >>= scalefactor;
            v0 = (v0 & mask0) << offset;
            for (int i = 0, j = 0; j < iminfo.SamplesPerRow; j++)
            {
                v = src[j];
                if (scaled)
                    v >>= scalefactor;
                dst[i] |= (v & mask0) << offset;
                offset -= bitDepth;
                if (offset < 0)
                {
                    offset = offset0;
                    i++;
                    dst[i] = 0;
                }
            }
            dst[0] |= v0;
        }

        // size original: samplesPerRow sizeFinal: samplesPerRowPacked (trailing elements are trash!)
        internal static void PackInplaceBytes(ImageInfo iminfo, byte[] source, byte[] destination, bool scaled)
        {
            int bitDepth = iminfo.BitDepth;
            if (bitDepth >= 8)
                return; // nothing to do

            byte mask0 = (byte)GetMaskForPackedFormatsLs(bitDepth);
            byte scalefactor = (byte)(8 - bitDepth);
            byte offset0 = (byte)(8 - bitDepth);
            byte v;
            byte v0;
            int offset = 8 - bitDepth;
            v0 = source[0]; // first value is special
            destination[0] = 0;
            if (scaled)
                v0 >>= scalefactor;

            v0 = (byte)((v0 & mask0) << offset);
            for (int i = 0, j = 0; j < iminfo.SamplesPerRow; j++)
            {
                v = source[j];
                if (scaled)
                    v >>= scalefactor;
                destination[i] |= (byte)((v & mask0) << offset);
                offset -= bitDepth;
                if (offset < 0)
                {
                    offset = offset0;
                    i++;
                    destination[i] = 0;
                }
            }
            destination[0] |= v0;
        }

        public ImageLine UnpackToNewImageLine()
        {
            var newline = new ImageLine(ImgInfo, LineSampleType, true);
            if (LineSampleType == SampleType.Integer)
                UnpackInplaceInts(ImgInfo, ScanlineInts, newline.ScanlineInts, false);
            else
                UnpackInplaceBytes(ImgInfo, ScanlineBytes, newline.ScanlineBytes, false);
            return newline;
        }

        public ImageLine PackToNewImageLine()
        {
            var newline = new ImageLine(ImgInfo, LineSampleType, false);
            if (LineSampleType == SampleType.Integer)
                PackInplaceInts(ImgInfo, ScanlineInts, newline.ScanlineInts, false);
            else
                PackInplaceBytes(ImgInfo, ScanlineBytes, newline.ScanlineBytes, false);
            return newline;
        }

        #endregion

        #region Scanlines

        /// <summary>
        /// Makes a deep copy.
        /// </summary>
        /// <remarks>You should rarely use this.</remarks>
        internal void SetScanlineIntsCopy(int[] buffer)
            => Array.Copy(buffer, 0, ScanlineInts, 0, ScanlineInts.Length);

        /// <summary>
        /// Makes a deep copy.
        /// </summary>
        /// <remarks>You should rarely use this.</remarks>
        internal int[] GetScanlineIntsCopy(int[] buffer)
        {
            if (buffer == null || buffer.Length < ScanlineInts.Length)
                buffer = new int[ScanlineInts.Length];

            Array.Copy(ScanlineInts, 0, buffer, 0, ScanlineInts.Length);
            return buffer;
        }

        /// <summary>
        /// Gets the current scanline directly.
        /// </summary>
        public int[] GetScanlineInts()
            => ScanlineInts;

        /// <summary>
        /// Gets the current scanline directly.
        /// </summary>
        public byte[] GetScanlineBytes()
            => ScanlineBytes;

        /// <summary>
        /// Gets the current scanline, or an unpacked copy if necessary.
        /// </summary>
        /// <returns>The current scanline or an unpacked copy.</returns>
        public int[] GetUnpackedScanlineInts()
        {
            int[] line;
            if (ImgInfo.Packed)
            {
                line = new int[ImgInfo.SamplesPerRow];
                UnpackInplaceInts(ImgInfo, ScanlineInts, line, false);
            }
            else
            {
                line = ScanlineInts;
            }

            return line;
        }

        /// <summary>
        /// Gets the current scanline, or an unpacked copy if necessary.
        /// </summary>
        /// <returns>The current scanline or an unpacked copy.</returns>
        public byte[] GetUnpackedScanlineBytes()
        {
            byte[] line;
            if (ImgInfo.Packed)
            {
                line = new byte[ImgInfo.SamplesPerRow];
                UnpackInplaceBytes(ImgInfo, ScanlineBytes, line, false);
            }
            else
            {
                line = ScanlineBytes;
            }

            return line;
        }

        /// <summary>
        /// Sets the current scanline, packing then using a copy if necessary.
        /// </summary>
        /// <param name="line">The line to pack, will not be copied if no packing is needed.</param>
        public void SetUnpackedScanlineInts(int[] line)
        {
            if (ImgInfo.Packed)
            {
                PackInplaceInts(ImgInfo, line, ScanlineInts, false);
            }
            else
            {
                ScanlineInts = line;
            }
        }

        /// <summary>
        /// Sets the current scanline, packing then using a copy if necessary.
        /// </summary>
        /// <param name="line">The line to pack, will not be copied if no packing is needed.</param>
        public void SetUnpackedScanlineBytes(byte[] line)
        {
            if (ImgInfo.Packed)
            {
                PackInplaceBytes(ImgInfo, line, ScanlineBytes, false);
            }
            else
            {
                ScanlineBytes = line;
            }
        }

        #endregion

        #region RGB Helpers

        public bool IsRGB()
            => ImgInfo.Channels >= 3;

        public bool IsRGBA()
            => ImgInfo.Channels > 3 && ImgInfo.HasAlpha;

        public Rgb24[] GetRgb24()
        {
            if (ImgInfo.Channels < 3)
                throw new InvalidOperationException("This is only supported on RGB images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(Rgb24)}, please use {nameof(Rgb48)} or {nameof(Rgba64)} instead.");

            if (ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(Rgba32)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                return ColorConvert.FromChannelIntsToRgb24(GetUnpackedScanlineInts());
            }
            else if (LineSampleType == SampleType.Byte)
            {
                return ColorConvert.FromChannelBytesToRgb24(GetUnpackedScanlineBytes());
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetRgb24)}");
            }
        }

        public void SetRgb24(Rgb24[] colors)
        {
            if (ImgInfo.Channels < 3)
                throw new InvalidOperationException("This is only supported on RGB images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(Rgb24)}, please use {nameof(Rgb48)} or {nameof(Rgba64)} instead.");

            if (ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(Rgba32)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                SetUnpackedScanlineBytes(ColorConvert.ToChannelBytes(colors));
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetRgb24)}");
            }
        }

        public Rgba32[] GetRgba32()
        {
            if (ImgInfo.Channels < 4)
                throw new InvalidOperationException("This is only supported on RGBA images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(Rgba32)}, please use {nameof(Rgb48)} or {nameof(Rgba64)} instead.");

            if (!ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image does not have an alpha channel, please use {nameof(Rgb24)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                return ColorConvert.FromChannelIntsToRgba32(GetUnpackedScanlineInts());
            }
            else if (LineSampleType == SampleType.Byte)
            {
                return ColorConvert.FromChannelBytesToRgba32(GetUnpackedScanlineBytes());
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetRgba32)}");
            }
        }

        public void SetRgba32(Rgba32[] colors)
        {
            if (ImgInfo.Channels < 4)
                throw new InvalidOperationException("This is only supported on RGBA images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(Rgba32)}, please use {nameof(Rgb48)} or {nameof(Rgba64)} instead.");

            if (!ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image does not have an alpha channel, please use {nameof(Rgb24)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                SetUnpackedScanlineBytes(ColorConvert.ToChannelBytes(colors));
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetRgba32)}");
            }
        }

        public Rgb48[] GetRgb48()
        {
            if (ImgInfo.Channels < 3)
                throw new InvalidOperationException("This is only supported on RGB images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(Rgb48)}, please use {nameof(Rgb24)} or {nameof(Rgba32)} instead.");

            if (ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image has an alpha channel, please use {nameof(Rgba64)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                return ColorConvert.FromChannelIntsToRgb48(GetUnpackedScanlineInts());
            }
            else if (LineSampleType == SampleType.Byte)
            {
                return ColorConvert.FromChannelBytesToRgb48(GetUnpackedScanlineBytes());
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetRgb48)}");
            }
        }

        public void SetRgb48(Rgb48[] colors)
        {
            if (ImgInfo.Channels < 3)
                throw new InvalidOperationException("This is only supported on RGB images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(Rgb48)}, please use {nameof(Rgb24)} or {nameof(Rgba32)} instead.");

            if (ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image has an alpha channel, please use {nameof(Rgba64)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                throw new NotSupportedException($"Cannot set bytes to {nameof(Rgb48)}.");
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetRgb48)}");
            }
        }

        public Rgba64[] GetRgba64()
        {
            if (ImgInfo.Channels < 4)
                throw new InvalidOperationException("This is only supported on RGBA images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(Rgba64)}, please use {nameof(Rgb24)} or {nameof(Rgba32)} instead.");

            if (!ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image does not have an alpha channel, please use {nameof(Rgb48)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                return ColorConvert.FromChannelIntsToRgba64(GetUnpackedScanlineInts());
            }
            else if (LineSampleType == SampleType.Byte)
            {
                return ColorConvert.FromChannelBytesToRgba64(GetUnpackedScanlineBytes());
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetRgba64)}");
            }
        }

        public void SetRgba64(Rgba64[] colors)
        {
            if (ImgInfo.Channels < 4)
                throw new InvalidOperationException("This is only supported on RGBA images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(Rgba64)}, please use {nameof(Rgb24)} or {nameof(Rgba32)} instead.");

            if (!ImgInfo.HasAlpha)
                throw new InvalidOperationException($"Image does not have an alpha channel, please use {nameof(Rgb48)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                throw new NotSupportedException($"Cannot set bytes to {nameof(Rgba64)}.");
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetRgba64)}");
            }
        }

        #endregion

        #region Grayscale Helpers

        public bool IsGrayScale()
            => ImgInfo.Grayscale;

        public bool IsGrayScaleAlpha()
            => ImgInfo.Grayscale && ImgInfo.HasAlpha;

        public L8[] GetL8()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(L8)}, please use {nameof(L16)} or {nameof(La32)} instead.");

            if (ImgInfo.Channels > 1)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(La16)} instead.");

            L8[] colors = new L8[ImgInfo.Columns];
            if (LineSampleType == SampleType.Integer)
            {
                int[] line = GetUnpackedScanlineInts();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new L8((byte)line[i]);
                }
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = GetUnpackedScanlineBytes();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new L8(line[i]);
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetL8)}");
            }

            return colors;
        }

        public void SetL8(L8[] colors)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(L8)}, please use {nameof(L16)} or {nameof(La32)} instead.");

            if (ImgInfo.Channels > 1)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(La16)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                SetUnpackedScanlineBytes(ColorConvert.ToChannelBytes(colors));
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetL8)}");
            }
        }

        public L16[] GetL16()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(L16)}, please use {nameof(L8)} or {nameof(La16)} instead.");

            if (ImgInfo.Channels > 1)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(La32)} instead.");

            L16[] colors = new L16[ImgInfo.Columns];
            if (LineSampleType == SampleType.Integer)
            {
                int[] line = GetUnpackedScanlineInts();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new L16((ushort)line[i]);
                }
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = GetUnpackedScanlineBytes();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new L16(line[i]);
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetL16)}");
            }

            return colors;
        }

        public void SetL16(L16[] colors)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(L16)}, please use {nameof(L8)} or {nameof(La16)} instead.");

            if (ImgInfo.Channels > 1)
                throw new InvalidOperationException($"Image has alpha channel, please use {nameof(La32)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                throw new NotSupportedException($"Cannot set {nameof(L16)} to bytes.");
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetL16)}");
            }
        }

        public La16[] GetLa16()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(La16)}, please use {nameof(L16)} or {nameof(La32)} instead.");

            if (ImgInfo.Channels > 1)
                throw new InvalidOperationException($"Image does not have alpha channel, please use {nameof(L8)} instead.");

            La16[] colors = new La16[ImgInfo.Columns];
            int valueIndex = 0;
            if (LineSampleType == SampleType.Integer)
            {
                int[] line = GetUnpackedScanlineInts();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new La16((byte)line[valueIndex++], (byte)line[valueIndex++]);
                }
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = GetUnpackedScanlineBytes();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new La16(ScanlineBytes[valueIndex++], ScanlineBytes[valueIndex++]);
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetLa16)}");
            }

            return colors;
        }

        public void SetLa16(La16[] colors)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth > 8)
                throw new InvalidOperationException($"Bitdepth too large for {nameof(La16)}, please use {nameof(L16)} or {nameof(La32)} instead.");

            if (ImgInfo.Channels < 2)
                throw new InvalidOperationException($"Image does not have alpha channel, please use {nameof(L8)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                SetUnpackedScanlineBytes(ColorConvert.ToChannelBytes(colors));
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetLa16)}");
            }
        }

        public La32[] GetLa32()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(La32)}, please use {nameof(L8)} or {nameof(La16)} instead.");

            if (ImgInfo.Channels < 2)
                throw new InvalidOperationException($"Image does not have alpha channel, please use {nameof(L16)} instead.");

            La32[] colors = new La32[ImgInfo.Columns];
            int valueIndex = 0;
            if (LineSampleType == SampleType.Integer)
            {
                int[] line = GetUnpackedScanlineInts();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new La32((ushort)line[valueIndex++], (ushort)line[valueIndex++]);
                }
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = GetUnpackedScanlineBytes();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    colors[i] = new La32(line[valueIndex++], line[valueIndex++]);
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetLa32)}");
            }

            return colors;
        }

        public void SetLa32(La32[] colors)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException($"This is only supported by grayscale images.");

            if (ImgInfo.BitDepth < 16)
                throw new InvalidOperationException($"Bitdepth too small for {nameof(La32)}, please use {nameof(L8)} or {nameof(La16)} instead.");

            if (ImgInfo.Channels < 2)
                throw new InvalidOperationException($"Image does not have alpha channel, please use {nameof(L16)} instead.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(ColorConvert.ToChannelInts(colors));
            }
            else if (LineSampleType == SampleType.Byte)
            {
                throw new NotSupportedException($"Cannot set {nameof(La32)} to bytes.");
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetLa32)}");
            }
        }

        #endregion

        #region Palette Index Helpers

        public bool IsIndexed()
            => ImgInfo.Indexed;

        public bool IsIndexedAlpha()
            => ImgInfo.Indexed && ImgInfo.HasAlpha;

        public int[] GetIndices()
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException($"This is only supported by indexed/palette images.");

            // Get unpacked indices
            int[] indices = new int[ImgInfo.Columns];
            if (LineSampleType == SampleType.Integer)
            {
                int[] line = GetUnpackedScanlineInts();
                Array.Copy(line, indices, ImgInfo.Columns);
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = GetUnpackedScanlineBytes();
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    indices[i] = line[i];
                }
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(GetIndices)}");
            }

            return indices;
        }

        public void SetIndices(int[] indices)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException($"This is only supported by indexed/palette images.");

            if (LineSampleType == SampleType.Integer)
            {
                SetUnpackedScanlineInts(indices);
            }
            else if (LineSampleType == SampleType.Byte)
            {
                byte[] line = new byte[ImgInfo.SamplesPerRow];
                for (int i = 0; i < ImgInfo.Columns; i++)
                {
                    line[i] = (byte)indices[i];
                }
                SetUnpackedScanlineBytes(line);
            }
            else
            {
                throw new NotImplementedException($"{nameof(LineSampleType)} {LineSampleType} is not implemented for: {nameof(SetIndices)}");
            }
        }

        #endregion

        #region Methods

        public bool IsInt()
            => LineSampleType == SampleType.Integer;

        public bool IsByte()
            => LineSampleType == SampleType.Byte;

        public override string ToString()
            => $"ImageLine [RowNum={RowNum}, Columns={ImgInfo.Columns}, BitsPerChannel={ImgInfo.BitDepth}, Size={ScanlineInts.Length}]";

        #endregion
    }
}
