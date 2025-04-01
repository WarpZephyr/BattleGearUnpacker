namespace Pngcs.Chunks
{
    /// <summary>
    /// Unknown (for our chunk factory) chunk type.
    /// </summary>
    public class PngChunkUNKNOWN : PngChunkMultiple
    {
        private byte[] _data;

        public PngChunkUNKNOWN(string id, ImageInfo info)
            : base(id, info)
        {
            _data = [];
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NONE;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw p = CreateEmptyChunk(_data.Length, false);
            p.Data = _data;
            return p;
        }

        public override void ParseFromRaw(ChunkRaw c)
            => _data = c.Data;

        // does not copy!
        public byte[] GetData()
            => _data;

        // does not copy!
        public void SetData(byte[] data)
            => _data = data;

        public override void CloneDataFromRead(PngChunk other)
        {
            // THIS SHOULD NOT BE CALLED IF ALREADY CLONED WITH COPY CONSTRUCTOR
            PngChunkUNKNOWN c = (PngChunkUNKNOWN)other;
            _data = c._data; // not deep copy
        }
    }
}
