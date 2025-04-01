using Pngcs.IO.Zlib;

namespace Pngcs.IO
{
    /// <summary>
    /// A configuration for a <see cref="PngWriter"/>.
    /// </summary>
    public class PngWriterConfig
    {
        /// <summary>
        /// The default config.
        /// </summary>
        public static PngWriterConfig Default => new PngWriterConfig();

        /// <summary>
        /// Deflate algortithm compression strategy.
        /// </summary>
        public DeflateCompressStrategy CompressionStrategy { get; set; }

        /// <summary>
        /// The internal prediction filter type, or a strategy to choose it.
        /// </summary>
        public FilterType FilterType { get; set; }

        /// <summary>
        /// Zip compression level (0 - 9).
        /// </summary>
        /// <remarks>
        /// Default is 6.<br/>
        /// Maximum is 9.
        /// </remarks>
        public int CompressionLevel { get; set; }

        /// <summary>
        /// The maximum size of IDAT chunks.
        /// </summary>
        /// <remarks>
        /// 0 uses default (PngIDatChunkOutputStream 32768).
        /// </remarks>
        public int IdatMaxSize { get; set; }

        /// <summary>
        /// This determines whether or not packed values (bitdepths 1,2,4) will be written unpacked.
        /// </summary>
        public bool UnpackedMode { get; set; }

        /// <summary>
        /// Whether or not the base stream should be left open.
        /// </summary>
        public bool LeaveOpen { get; set; }

        public PngWriterConfig()
        {
            // Default settings
            CompressionLevel = 6;
            LeaveOpen = false;
            IdatMaxSize = 0; // use default
            CompressionStrategy = DeflateCompressStrategy.Filtered;
            FilterType = FilterType.FILTER_DEFAULT;
        }
    }
}
