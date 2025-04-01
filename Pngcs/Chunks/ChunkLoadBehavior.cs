using Pngcs.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Defines what to do with non-critical chunks when reading.
    /// </summary>
    public enum ChunkLoadBehavior
    {
        /// <summary>
        /// All non-critical chunks are skipped.
        /// </summary>
        LOAD_CHUNK_NEVER,

        /// <summary>
        /// Load chunk if 'known' (registered with the factory).
        /// </summary>
        LOAD_CHUNK_KNOWN,

        /// <summary>
        /// Load chunk if 'known' or safe to copy.
        /// </summary>
        LOAD_CHUNK_IF_SAFE,

        /// <summary>
        /// Load chunks always.<br/>
        /// Notice that other restrictions might apply, see <see cref="PngReader.SkipChunkMaxSize"/> <see cref="PngReader.SkipChunkIds"/>.
        /// </summary>
        LOAD_CHUNK_ALWAYS
    }
}
