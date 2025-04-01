namespace Pngcs.Chunks
{
    /// <summary>
    /// IEND chunk: http://www.w3.org/TR/PNG/#11IEND
    /// </summary>
    public class PngChunkIEND : PngChunkSingle
    {
        public const string ID = ChunkHelper.IEND;

        public PngChunkIEND(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NA;

        public override ChunkRaw CreateRawChunk()
            => new ChunkRaw(0, ChunkHelper.IEND_BYTES, false);

        public override void ParseFromRaw(ChunkRaw c) { }

        public override void CloneDataFromRead(PngChunk other) { }
    }
}
