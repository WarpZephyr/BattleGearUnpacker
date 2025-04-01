using Pngcs.Chunks;
using System.Collections.Generic;

namespace Pngcs.IO
{
    /// <summary>
    /// A configuration for a <see cref="PngReader"/>.
    /// </summary>
    public class PngReaderConfig
    {
        /// <summary>
        /// The default config.
        /// </summary>
        public static PngReaderConfig Default => new PngReaderConfig();

        /// <summary>
        /// Strategy for chunk loading. Default: LOAD_CHUNK_ALWAYS
        /// </summary>
        public ChunkLoadBehavior ChunkLoadBehaviour { get; set; }

        /// <summary>
        /// Maximum amount of bytes from ancillary chunks to load in memory 
        /// </summary>
        /// <remarks>
        ///  Default: 5MB. 0: unlimited. If exceeded, chunks will be skipped
        /// </remarks>
        public long MaxBytesMetadata { get; set; }

        /// <summary>
        /// Maximum total bytes to read from stream 
        /// </summary>
        /// <remarks>
        ///  Default: 200MB. 0: Unlimited. If exceeded, an exception will be thrown
        /// </remarks>
        public long MaxTotalBytesRead { get; set; }

        /// <summary>
        /// Maximum ancillary chunk size
        /// </summary>
        /// <remarks>
        ///  Default: 2MB, 0: unlimited. Chunks exceeding this size will be skipped (nor even CRC checked)
        /// </remarks>
        public int SkipChunkMaxSize { get; set; }

        /// <summary>
        /// Ancillary chunks to skip
        /// </summary>
        /// <remarks>
        ///  Default: { "fdAT" }. chunks with these ids will be skipped (nor even CRC checked)
        /// </remarks>
        public List<string> SkipChunkIds { get; set; }

        /// <summary>
        /// Whether or not the CRC32 check is enabled.
        /// </summary>
        public bool CheckCrc32 { get; set; }

        /// <summary>
        /// Whether or not to unpack bitdepths of 1, 2, or 4.
        /// </summary>
        public bool UnpackedMode { get; set; }

        /// <summary>
        /// Whether or not the base stream should be left open.
        /// </summary>
        public bool LeaveOpen { get; set; }

        public PngReaderConfig()
        {
            LeaveOpen = false;
            MaxBytesMetadata = 5 * 1024 * 1024;
            MaxTotalBytesRead = 200 * 1024 * 1024; // 200MB
            SkipChunkMaxSize = 2 * 1024 * 1024;
            SkipChunkIds = ["fdAT"];
            ChunkLoadBehaviour = ChunkLoadBehavior.LOAD_CHUNK_ALWAYS;
            CheckCrc32 = true;
            UnpackedMode = false;
        }
    }
}
