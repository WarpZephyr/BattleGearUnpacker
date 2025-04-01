using Pngcs.Chunks;
using System;

namespace Pngcs
{
    /// <summary>
    /// Simple immutable wrapper for basic image info.
    /// </summary>
    /// <remarks>
    /// Some parameters are clearly redundant.<br/>
    /// The constructor requires an 'ortogonal' subset.
    /// http://www.w3.org/TR/PNG/#11IHDR
    /// </remarks>
    public class ImageInfo
    {
        private const int MAX_COLS_ROWS_VAL = 400000; // very big value, but not as ridiculous as 2^32.

        /// <summary>
        /// Image width, in pixels
        /// </summary>
        public readonly int Columns;

        /// <summary>
        /// Image height, in pixels
        /// </summary>
        public readonly int Rows;

        /// <summary>
        /// Bits per sample (per channel) in the buffer. 
        /// </summary>
        /// <remarks>
        /// This is 8 or 16 for RGB/RGBA images. 
        /// For grayscale, it's 8 (or 1 2 4 ).
        /// For indexed images, number of bits per palette index (1 2 4 8).
        ///</remarks>
        public readonly int BitDepth;

        /// <summary>
        /// The number of channels used in the buffer .
        /// </summary>
        /// <remarks>
        /// WARNING: This is 3-4 for rgb/rgba, but 1 for indexed/grayscale.
        ///</remarks>
        public readonly int Channels;

        /// <summary>
        /// Bits used for each pixel in the buffer 
        /// </summary>
        /// <remarks>equals <c>channels * bitDepth</c>
        /// </remarks>
        public readonly int BitsPerPixel;

        /// <summary>
        /// Bytes per pixel, rounded up
        /// </summary>
        /// <remarks>This is mainly for internal use (filter)</remarks>
        public readonly int BytesPerPixel;

        /// <summary>
        /// Bytes per row, rounded up
        /// </summary>
        /// <remarks>equals <c>ceil(bitspp*cols/8)</c></remarks>
        public readonly int BytesPerRow;

        /// <summary>
        /// Samples (scalar values) per row
        /// </summary>
        /// <remarks>
        /// Equals <c>cols * channels</c>
        /// </remarks>
        public readonly int SamplesPerRow;

        /// <summary>
        /// Number of values in our scanline, which might be packed.
        /// </summary>
        /// <remarks>
        /// Equals samplesPerRow if not packed. Elsewhere, it's lower
        /// For internal use, mostly.
        /// </remarks>
        public readonly int SamplesPerRowPacked;

        /// <summary>
        /// Whether or not the image has an alpha channel.
        /// </summary>
        public readonly bool HasAlpha;

        /// <summary>
        /// Whether or not the image is grayscale (G/GA)
        /// </summary>
        public readonly bool Grayscale;

        /// <summary>
        /// Whether or not the image is indexed and uses a palette.
        /// </summary>
        public readonly bool Indexed;

        /// <summary>
        /// Whether or not there is less than one byte per sample (bitdepths 1,2,4).
        /// </summary>
        public readonly bool Packed;

        /// <summary>
        /// Makes a new RGB/RGBA <see cref="ImageInfo"/> with the specified settings.
        /// </summary>
        /// <param name="columns">Width in pixels</param>
        /// <param name="rows">Height in pixels</param>
        /// <param name="bitdepth">Bits per sample per channel</param>
        /// <param name="hasAlpha">Whether or not it should have an alpha channel.</param>
        public ImageInfo(int columns, int rows, int bitdepth, bool hasAlpha)
            : this(columns, rows, bitdepth, hasAlpha, false, false) { }

        /// <summary>
        /// Makes a new <see cref="ImageInfo"/> with the specified settings.
        /// </summary>
        /// <param name="columns">Width in pixels</param>
        /// <param name="rows">Height in pixels</param>
        /// <param name="bitdepth">Bits per sample per channel</param>
        /// <param name="hasAlpha">Whether or not it should have an alpha channel.</param>
        /// <param name="grayscale">Whether or not it should be grayscale.</param>
        /// <param name="indexed">Whether or not it has a palette and is indexed.</param>
        public ImageInfo(int columns, int rows, int bitdepth, bool hasAlpha, bool grayscale, bool indexed)
        {
            if (grayscale && indexed)
                throw new ArgumentException("Palette and grayscale are exclusive.");
            if (hasAlpha && indexed)
                throw new ArgumentException($"Store alpha in a {nameof(PngChunkTRNS)} for indexed, do not mark alpha here for indexed.");

            Columns = columns;
            Rows = rows;
            HasAlpha = hasAlpha;
            Indexed = indexed;
            Grayscale = grayscale;
            Channels = (grayscale || indexed) ? (hasAlpha ? 2 : 1) : (hasAlpha ? 4 : 3);

            // http://www.w3.org/TR/PNG/#11IHDR
            BitDepth = bitdepth;
            Packed = bitdepth < 8;
            BitsPerPixel = Channels * BitDepth;
            BytesPerPixel = (BitsPerPixel + 7) / 8;
            BytesPerRow = (BitsPerPixel * columns + 7) / 8;
            SamplesPerRow = Channels * Columns;
            SamplesPerRowPacked = Packed ? BytesPerRow : SamplesPerRow;

            // Checks
            switch (BitDepth)
            {
                case 1:
                case 2:
                case 4:
                    if (!(Indexed || Grayscale))
                        throw new NotSupportedException($"Only indexed or grayscale can use this bitdepth: {BitDepth}");
                    break;
                case 8:
                    break;
                case 16:
                    if (Indexed)
                        throw new NotSupportedException($"Indexed can't use this bitdepth: {BitDepth}");
                    break;
                default:
                    throw new NotSupportedException($"Invalid bitdepth: {BitDepth}");
            }

            if (columns < 1 || columns > MAX_COLS_ROWS_VAL)
                throw new ArgumentException($"Invalid column count: {columns}", nameof(columns));

            if (rows < 1 || rows > MAX_COLS_ROWS_VAL)
                throw new ArgumentException($"Invalid row count: {rows}", nameof(rows));
        }

        /// <summary>
        /// Gets a string representing the image info.
        /// </summary>
        /// <returns>A summary of the image info.</returns>
        public override string ToString()
        {
            return $"{nameof(ImageInfo)} [{nameof(Columns)}={Columns}, " +
                               $"{nameof(Rows)}={Rows}, " +
                               $"{nameof(BitDepth)}={BitDepth}, " +
                               $"{nameof(Channels)}={Channels}, " +
                               $"{nameof(BitsPerPixel)}={BitsPerPixel}, " +
                               $"{nameof(BytesPerPixel)}={BytesPerPixel}, " +
                               $"{nameof(BytesPerRow)}={BytesPerRow}, " +
                               $"{nameof(SamplesPerRow)}={SamplesPerRow}, " +
                               $"{nameof(SamplesPerRowPacked)}={SamplesPerRowPacked}, " +
                               $"{nameof(HasAlpha)}={HasAlpha}, " +
                               $"{nameof(Grayscale)}={Grayscale}, " +
                               $"{nameof(Indexed)}={Indexed}, " +
                               $"{nameof(Packed)}={Packed}]";
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + (HasAlpha ? 1231 : 1237);
            result = prime * result + BitDepth;
            result = prime * result + Channels;
            result = prime * result + Columns;
            result = prime * result + (Grayscale ? 1231 : 1237);
            result = prime * result + (Indexed ? 1231 : 1237);
            result = prime * result + Rows;
            return result;
        }

        public override bool Equals(object? obj)
        {
            if (this == obj)
                return true;

            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            ImageInfo other = (ImageInfo)obj;
            if (HasAlpha != other.HasAlpha)
                return false;

            if (BitDepth != other.BitDepth)
                return false;

            if (Channels != other.Channels)
                return false;

            if (Columns != other.Columns)
                return false;

            if (Grayscale != other.Grayscale)
                return false;

            if (Indexed != other.Indexed)
                return false;

            if (Rows != other.Rows)
                return false;

            return true;
        }
    }
}
