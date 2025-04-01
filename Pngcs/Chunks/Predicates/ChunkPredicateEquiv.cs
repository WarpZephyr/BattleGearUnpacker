namespace Pngcs.Chunks
{
    /// <summary>
    /// An ad-hoc criterion, perhaps useful, for equivalence.
    /// <see cref="ChunkHelper.Equivalent(PngChunk,PngChunk)"/> 
    /// </summary>
    internal class ChunkPredicateEquiv : IChunkPredicate
    {
        private readonly PngChunk _chunk;

        /// <summary>
        /// Creates predicate based of reference chunk
        /// </summary>
        /// <param name="chunk"></param>
        public ChunkPredicateEquiv(PngChunk chunk)
        {
            _chunk = chunk;
        }

        /// <summary>
        /// Check for match
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Matches(PngChunk c)
            => ChunkHelper.Equivalent(c, _chunk);
    }
}
