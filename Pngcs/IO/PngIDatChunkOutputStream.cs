using Pngcs.Chunks;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    /// The output stream for the IDAT chunk, fragmented at a fixed size (32k default).
    /// </summary>
    internal class PngIDatChunkOutputStream : ProgressiveOutputStream
    {
        private const int SIZE_DEFAULT = 32768; // 32k
        private readonly Stream _outputStream;

        public PngIDatChunkOutputStream(Stream output)
            : this(output, SIZE_DEFAULT) { }

        public PngIDatChunkOutputStream(Stream output, int size) : base(size > 8 ? size : SIZE_DEFAULT)
        {
            _outputStream = output;
        }

        protected override void FlushBuffer(byte[] buffer, int length)
        {
            var chunk = new ChunkRaw(length, ChunkHelper.IDAT_BYTES, false)
            {
                Data = buffer
            };

            chunk.WriteChunk(_outputStream);
        }

        // Closing the IDAT stream only flushes it, it does not close the underlying stream.
        public override void Close()
            => Flush();
    }
}
