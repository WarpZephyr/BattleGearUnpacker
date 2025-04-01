using System;

namespace Pngcs.Chunks
{
    class PngChunkSkipped : PngChunk
    {
        internal PngChunkSkipped(string id, ImageInfo imgInfo, int clen) : base(id, imgInfo)
        {
            Length = clen;
        }

        public sealed override bool AllowsMultiple()
            => true;

        public sealed override ChunkRaw CreateRawChunk()
            => throw new NotSupportedException("Not supported for a skipped chunk.");

        public sealed override void ParseFromRaw(ChunkRaw c)
            => throw new NotSupportedException("Not supported for a skipped chunk.");

        public sealed override void CloneDataFromRead(PngChunk other)
            => throw new NotSupportedException("Not supported for a skipped chunk.");

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NONE;
    }
}
