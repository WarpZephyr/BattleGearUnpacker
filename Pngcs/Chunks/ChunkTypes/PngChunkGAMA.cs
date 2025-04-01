using Pngcs.Helpers;
using System;

namespace Pngcs.Chunks
{
    /// <summary>
    /// gAMA chunk, see http://www.w3.org/TR/PNG/#11gAMA
    /// </summary>
    public class PngChunkGAMA : PngChunkSingle
    {
        public const string ID = ChunkHelper.gAMA;

        public double Gamma { get; set; }

        public PngChunkGAMA(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = CreateEmptyChunk(4, true);
            int g = (int)(Gamma * 100000 + 0.5d);
            PngHelperInternal.WriteInt32(g, c.Data, 0);
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk)
        {
            if (chunk.Length != 4)
                throw new Exception($"Bad chunk: {chunk}");

            int g = PngHelperInternal.ReadInt32(chunk.Data, 0);
            Gamma = g / 100000.0D;
        }

        public override void CloneDataFromRead(PngChunk other)
            => Gamma = ((PngChunkGAMA)other).Gamma;
    }
}
