using Pngcs.Helpers;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// sBIT chunk: http://www.w3.org/TR/PNG/#11sBIT
    /// 
    /// this chunk structure depends on the image type
    /// </summary>
    public class PngChunkSBIT : PngChunkSingle
    {
        public const string ID = ChunkHelper.sBIT;

        //	significant bits
        public int Graysb { get; set; }
        public int Alphasb { get; set; }
        public int Redsb { get; set; }
        public int Greensb { get; set; }
        public int Bluesb { get; set; }

        public PngChunkSBIT(ImageInfo info)
            : base(ID, info) { }


        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;


        public override void ParseFromRaw(ChunkRaw c)
        {
            if (c.Length != GetLength())
                throw new InvalidDataException($"Invalid chunk length: {c}");

            if (ImgInfo.Grayscale)
            {
                Graysb = PngHelperInternal.ReadByte(c.Data, 0);
                if (ImgInfo.HasAlpha)
                    Alphasb = PngHelperInternal.ReadByte(c.Data, 1);
            }
            else
            {
                Redsb = PngHelperInternal.ReadByte(c.Data, 0);
                Greensb = PngHelperInternal.ReadByte(c.Data, 1);
                Bluesb = PngHelperInternal.ReadByte(c.Data, 2);
                if (ImgInfo.HasAlpha)
                    Alphasb = PngHelperInternal.ReadByte(c.Data, 3);
            }
        }

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = CreateEmptyChunk(GetLength(), true);
            if (ImgInfo.Grayscale)
            {
                c.Data[0] = (byte)Graysb;
                if (ImgInfo.HasAlpha)
                    c.Data[1] = (byte)Alphasb;
            }
            else
            {
                c.Data[0] = (byte)Redsb;
                c.Data[1] = (byte)Greensb;
                c.Data[2] = (byte)Bluesb;
                if (ImgInfo.HasAlpha)
                    c.Data[3] = (byte)Alphasb;
            }
            return c;
        }


        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkSBIT otherx = (PngChunkSBIT)other;
            Graysb = otherx.Graysb;
            Redsb = otherx.Redsb;
            Greensb = otherx.Greensb;
            Bluesb = otherx.Bluesb;
            Alphasb = otherx.Alphasb;
        }

        private int GetLength()
        {
            int len = ImgInfo.Grayscale ? 1 : 3;
            if (ImgInfo.HasAlpha)
                len += 1;
            return len;
        }
    }
}
