using Pngcs.Helpers;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// IHDR chunk: http://www.w3.org/TR/PNG/#11IHDR
    /// </summary>
    public class PngChunkIHDR : PngChunkSingle
    {
        public const string ID = ChunkHelper.IHDR;
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int BitsPerChannel { get; set; }
        public int ColorModel { get; set; }
        public int CompressionMethod { get; set; }
        public int FilterMethod { get; set; }
        public int Interlaced { get; set; }

        public PngChunkIHDR(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NA;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = new ChunkRaw(13, ChunkHelper.IHDR_BYTES, true);

            int offset = 0;
            PngHelperInternal.WriteInt32(Columns, c.Data, offset);
            offset += 4;

            PngHelperInternal.WriteInt32(Rows, c.Data, offset);
            offset += 4;

            c.Data[offset++] = (byte)BitsPerChannel;
            c.Data[offset++] = (byte)ColorModel;
            c.Data[offset++] = (byte)CompressionMethod;
            c.Data[offset++] = (byte)FilterMethod;
            c.Data[offset++] = (byte)Interlaced;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            if (c.Length != 13)
                throw new InvalidDataException($"Bad IDHR length: {c.Length}");

            MemoryStream st = c.ToMemoryStream();
            Columns = PngHelperInternal.ReadInt32(st);
            Rows = PngHelperInternal.ReadInt32(st);
            // bit depth: number of bits per channel
            BitsPerChannel = PngHelperInternal.ReadByte(st);
            ColorModel = PngHelperInternal.ReadByte(st);
            CompressionMethod = PngHelperInternal.ReadByte(st);
            FilterMethod = PngHelperInternal.ReadByte(st);
            Interlaced = PngHelperInternal.ReadByte(st);
        }

        public override void CloneDataFromRead(PngChunk chunk)
        {
            PngChunkIHDR ihdrChunk = (PngChunkIHDR)chunk;
            Columns = ihdrChunk.Columns;
            Rows = ihdrChunk.Rows;
            BitsPerChannel = ihdrChunk.BitsPerChannel;
            ColorModel = ihdrChunk.ColorModel;
            CompressionMethod = ihdrChunk.CompressionMethod;
            FilterMethod = ihdrChunk.FilterMethod;
            Interlaced = ihdrChunk.Interlaced;
        }
    }
}
