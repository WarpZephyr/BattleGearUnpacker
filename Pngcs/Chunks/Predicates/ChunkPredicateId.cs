namespace Pngcs.Chunks
{
    /// <summary>
    /// Match if have same Chunk Id
    /// </summary>
    internal class ChunkPredicateId : IChunkPredicate
    {
        private readonly string _id;

        public ChunkPredicateId(string id)
        {
            _id = id;
        }

        public bool Matches(PngChunk c)
            => c.Id.Equals(_id);
    }
}