using Pngcs.Helpers;
using System;

namespace Pngcs.Chunks
{
    /// <summary>
    /// hIST chunk, see http://www.w3.org/TR/PNG/#11hIST
    /// Only for palette images
    /// </summary>
    public class PngChunkHIST : PngChunkSingle
    {
        public readonly static string ID = ChunkHelper.hIST;

        /// <summary>
        /// Should have the same length as the palette.
        /// </summary>
        public int[] Histogram { get; set; }  = new int[0];

        public PngChunkHIST(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed images accept a HIST chunk.");

            ChunkRaw c = CreateEmptyChunk(Histogram.Length * 2, true);
            for (int i = 0; i < Histogram.Length; i++)
            {
                PngHelperInternal.WriteInt16(Histogram[i], c.Data, i * 2);
            }
            return c;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed images accept a HIST chunk.");

            int nentries = c.Data.Length / 2;
            Histogram = new int[nentries];
            for (int i = 0; i < Histogram.Length; i++)
            {
                Histogram[i] = PngHelperInternal.ReadInt16(c.Data, i * 2);
            }
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkHIST otherx = (PngChunkHIST)other;
            Histogram = new int[otherx.Histogram.Length];
            Array.Copy(otherx.Histogram, 0, Histogram, 0, otherx.Histogram.Length);
        }
    }
}
