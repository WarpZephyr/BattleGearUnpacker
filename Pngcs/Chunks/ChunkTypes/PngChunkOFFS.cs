using Pngcs.Helpers;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// oFFs chunk: http://www.libpng.org/pub/png/spec/register/pngext-1.3.0-pdg.html#C.oFFs
    /// </summary>
    public class PngChunkOFFS : PngChunkSingle
    {
        public const string ID = "oFFs";

        public long PosX { get; set; }
        public long PosY { get; set; }

        /// <summary>
        /// 0 for pixel, 1 for micrometer.
        /// </summary>
        public int Units { get; set; }

        public PngChunkOFFS(ImageInfo info)
            : base(ID, info) { }


        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = CreateEmptyChunk(9, true);
            PngHelperInternal.WriteInt32((int)PosX, c.Data, 0);
            PngHelperInternal.WriteInt32((int)PosY, c.Data, 4);
            c.Data[8] = (byte)Units;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk)
        {
            if (chunk.Length != 9)
                throw new InvalidDataException($"Invalid chunk length: {chunk}");

            PosX = PngHelperInternal.ReadInt32(chunk.Data, 0);
            if (PosX < 0)
                PosX += 0x100000000L;

            PosY = PngHelperInternal.ReadInt32(chunk.Data, 4);
            if (PosY < 0)
                PosY += 0x100000000L;

            Units = PngHelperInternal.ReadByte(chunk.Data, 8);
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkOFFS otherx = (PngChunkOFFS)other;
            PosX = otherx.PosX;
            PosY = otherx.PosY;
            Units = otherx.Units;
        }
    }
}
