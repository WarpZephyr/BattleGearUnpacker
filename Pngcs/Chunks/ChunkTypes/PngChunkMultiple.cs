namespace Pngcs.Chunks
{
    /// <summary>
    /// A chunk type that allows duplicates of itself in an image.
    /// </summary>
    public abstract class PngChunkMultiple : PngChunk
    {
        internal PngChunkMultiple(string id, ImageInfo imgInfo)
            : base(id, imgInfo) { }

        public sealed override bool AllowsMultiple()
            => true;
    }
}
