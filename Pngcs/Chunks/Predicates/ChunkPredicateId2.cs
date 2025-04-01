namespace Pngcs.Chunks
{
    /// <summary>
    /// Match if have same id and, if Text (or SPLT) if have the same key.
    /// </summary>
    /// <remarks>
    /// This is the same as ChunkPredicateEquivalent, the only difference is that does not requires a chunk at construction time.
    /// </remarks>
    internal class ChunkPredicateId2 : IChunkPredicate
    {
        private readonly string _id;
        private readonly string _innerid;

        public ChunkPredicateId2(string id, string innerid)
        {
            _id = id;
            _innerid = innerid;
        }

        public bool Matches(PngChunk c)
        {
            if (!c.Id.Equals(_id))
                return false;

            if (c is PngChunkTextVar textVar && !textVar.GetKey().Equals(_innerid))
                return false;

            if (c is PngChunkSPLT sPLT && !sPLT.PalName.Equals(_innerid))
                return false;

            return true;
        }
    }
}
