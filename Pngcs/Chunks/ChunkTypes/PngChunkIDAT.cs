namespace Pngcs.Chunks
{
    /// <summary>
    /// IDAT chunk: http://www.w3.org/TR/PNG/#11IDAT<br/>
    /// 
    /// This object is dummy placeholder - We treat this chunk in a very different way than ancillary chnks.
    /// </summary>
    public class PngChunkIDAT : PngChunkMultiple
    {
        public const string ID = ChunkHelper.IDAT;

        public PngChunkIDAT(ImageInfo i, int len, long offset) : base(ID, i)
        {
            Length = len;
            Offset = offset;
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NA;

        public override ChunkRaw? CreateRawChunk()
            => null;

        public override void ParseFromRaw(ChunkRaw c) { }

        public override void CloneDataFromRead(PngChunk other) { }
    }
}
