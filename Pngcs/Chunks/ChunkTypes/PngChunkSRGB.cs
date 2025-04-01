using Pngcs.Helpers;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// sRGB chunk: http://www.w3.org/TR/PNG/#11sRGB
    /// </summary>
    public class PngChunkSRGB : PngChunkSingle
    {
        public const string ID = ChunkHelper.sRGB;

        public const int RENDER_INTENT_Perceptual = 0;
        public const int RENDER_INTENT_Relative_colorimetric = 1;
        public const int RENDER_INTENT_Saturation = 2;
        public const int RENDER_INTENT_Absolute_colorimetric = 3;

        public int Intent { get; set; }

        public PngChunkSRGB(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = CreateEmptyChunk(1, true);
            c.Data[0] = (byte)Intent;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk)
        {
            if (chunk.Length != 1)
                throw new InvalidDataException($"Invalid chunk length: {chunk}");

            Intent = PngHelperInternal.ReadByte(chunk.Data, 0);
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkSRGB otherx = (PngChunkSRGB)other;
            Intent = otherx.Intent;
        }
    }
}
