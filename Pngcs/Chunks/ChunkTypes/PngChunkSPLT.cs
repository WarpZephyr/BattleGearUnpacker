using Pngcs.Helpers;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// sPLT chunk: http://www.w3.org/TR/PNG/#11sPLT
    /// </summary>
    public class PngChunkSPLT : PngChunkMultiple
    {
        public const string ID = ChunkHelper.sPLT;

        /// <summary>
        /// Must be unique in image.
        /// </summary>
        public string PalName { get; set; }

        /// <summary>
        /// The sample depth, 8-16.
        /// </summary>
        public int SampleDepth { get; set; }

        /// <summary>
        /// A color palette with 5 elements per entry.
        /// </summary>
        public int[] Palette { get; set; }

        public PngChunkSPLT(ImageInfo info) : base(ID, info)
        {
            PalName = string.Empty;
            SampleDepth = 0;
            Palette = [];
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            var ba = new MemoryStream();
            ChunkHelper.WriteBytes(ba, ChunkHelper.ToBytes(PalName));
            ba.WriteByte(0); // separator
            ba.WriteByte((byte)SampleDepth);
            int length = GetLength();
            for (int n = 0; n < length; n++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (SampleDepth == 8)
                        PngHelperInternal.WriteByte(ba, (byte)Palette[n * 5 + i]);
                    else
                        PngHelperInternal.WriteInt16(ba, Palette[n * 5 + i]);
                }
                PngHelperInternal.WriteInt16(ba, Palette[n * 5 + 4]);
            }
            byte[] b = ba.ToArray();
            ChunkRaw chunk = CreateEmptyChunk(b.Length, false);
            chunk.Data = b;
            return chunk;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            int index = -1;
            for (int i = 0; i < c.Data.Length; i++)
            { // look for first zero
                if (c.Data[i] == 0)
                {
                    index = i;
                    break;
                }
            }

            if (index <= 0 || index > c.Data.Length - 2)
                throw new InvalidDataException("Invalid sPLT chunk: no terminator found.");

            PalName = ChunkHelper.ToString(c.Data, 0, index);
            SampleDepth = PngHelperInternal.ReadByte(c.Data, index + 1);
            index += 2;
            int nentries = (c.Data.Length - index) / (SampleDepth == 8 ? 6 : 10);
            Palette = new int[nentries * 5];
            int r, g, b, a, f, ne;
            ne = 0;
            for (int i = 0; i < nentries; i++)
            {
                if (SampleDepth == 8)
                {
                    r = PngHelperInternal.ReadByte(c.Data, index++);
                    g = PngHelperInternal.ReadByte(c.Data, index++);
                    b = PngHelperInternal.ReadByte(c.Data, index++);
                    a = PngHelperInternal.ReadByte(c.Data, index++);
                }
                else
                {
                    r = PngHelperInternal.ReadInt16(c.Data, index);
                    index += 2;
                    g = PngHelperInternal.ReadInt16(c.Data, index);
                    index += 2;
                    b = PngHelperInternal.ReadInt16(c.Data, index);
                    index += 2;
                    a = PngHelperInternal.ReadInt16(c.Data, index);
                    index += 2;
                }

                f = PngHelperInternal.ReadInt16(c.Data, index);
                index += 2;
                Palette[ne++] = r;
                Palette[ne++] = g;
                Palette[ne++] = b;
                Palette[ne++] = a;
                Palette[ne++] = f;
            }
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkSPLT otherx = (PngChunkSPLT)other;
            PalName = otherx.PalName;
            SampleDepth = otherx.SampleDepth;
            Palette = new int[otherx.Palette.Length];
            System.Array.Copy(otherx.Palette, 0, Palette, 0, Palette.Length);

        }

        public int GetLength()
            => Palette.Length / 5;

    }
}
