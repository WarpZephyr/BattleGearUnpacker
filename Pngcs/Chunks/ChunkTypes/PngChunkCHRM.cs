using Pngcs.Helpers;
using System;

namespace Pngcs.Chunks
{
    /// <summary>
    /// cHRM chunk, see http://www.w3.org/TR/PNG/#11cHRM
    /// </summary>
    public class PngChunkCHRM : PngChunkSingle
    {
        public const string ID = ChunkHelper.cHRM;

        private double whitex, whitey;
        private double redx, redy;
        private double greenx, greeny;
        private double bluex, bluey;

        public PngChunkCHRM(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw rawChunk = CreateEmptyChunk(32, true);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(whitex), rawChunk.Data, 0);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(whitey), rawChunk.Data, 4);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(redx), rawChunk.Data, 8);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(redy), rawChunk.Data, 12);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(greenx), rawChunk.Data, 16);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(greeny), rawChunk.Data, 20);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(bluex), rawChunk.Data, 24);
            PngHelperInternal.WriteInt32(PngHelperInternal.DoubleToInt100000(bluey), rawChunk.Data, 28);
            return rawChunk;
        }

        public override void ParseFromRaw(ChunkRaw rawChunk)
        {
            if (rawChunk.Length != 32)
                throw new Exception($"Bad chunk: {rawChunk}");

            whitex = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 0));
            whitey = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 4));
            redx = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 8));
            redy = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 12));
            greenx = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 16));
            greeny = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 20));
            bluex = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 24));
            bluey = PngHelperInternal.IntToDouble100000(PngHelperInternal.ReadInt32(rawChunk.Data, 28));
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkCHRM otherx = (PngChunkCHRM)other;
            whitex = otherx.whitex;
            whitey = otherx.whitex;
            redx = otherx.redx;
            redy = otherx.redy;
            greenx = otherx.greenx;
            greeny = otherx.greeny;
            bluex = otherx.bluex;
            bluey = otherx.bluey;
        }

        public void SetChromaticities(double whitex, double whitey,
            double redx, double redy,
            double greenx, double greeny,
            double bluex, double bluey)
        {
            this.whitex = whitex;
            this.redx = redx;
            this.greenx = greenx;
            this.bluex = bluex;
            this.whitey = whitey;
            this.redy = redy;
            this.greeny = greeny;
            this.bluey = bluey;
        }

        public double[] GetChromaticities()
            => new double[] { whitex, whitey, redx, redy, greenx, greeny, bluex, bluey };
    }
}
