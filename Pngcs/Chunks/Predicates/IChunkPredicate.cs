namespace Pngcs.Chunks
{
    /// <summary>
    /// Decides if another chunk "matches", according to some criterion.
    /// </summary>
    public interface IChunkPredicate
    {
        /// <summary>
        /// Whether or not another chunk matches with this one.
        /// </summary>
        /// <param name="chunk">The other chunk.</param>
        /// <returns>true if they match.</returns>
        bool Matches(PngChunk chunk);
    }
}
