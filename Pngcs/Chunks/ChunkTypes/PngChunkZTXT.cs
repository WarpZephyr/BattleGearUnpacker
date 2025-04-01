using System;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// zTXt chunk: http://www.w3.org/TR/PNG/#11zTXt
    /// </summary>
    public class PngChunkZTXT : PngChunkTextVar
    {
        public const string ID = ChunkHelper.zTXt;

        public PngChunkZTXT(ImageInfo info)
            : base(ID, info) { }

        public override ChunkRaw CreateRawChunk()
        {
            if (_key.Length == 0)
                throw new InvalidOperationException("Text chunk key must be non-empty.");
            MemoryStream ba = new MemoryStream();
            ChunkHelper.WriteBytes(ba, ChunkHelper.ToBytes(_key));
            ba.WriteByte(0); // separator
            ba.WriteByte(0); // compression method: 0
            byte[] textbytes = ChunkHelper.CompressBytes(ChunkHelper.ToBytes(_value), true);
            ChunkHelper.WriteBytes(ba, textbytes);
            byte[] b = ba.ToArray();
            ChunkRaw chunk = CreateEmptyChunk(b.Length, false);
            chunk.Data = b;
            return chunk;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            int nullTerm = -1;
            for (int i = 0; i < c.Data.Length; i++)
            { // look for first zero
                if (c.Data[i] != 0)
                    continue;
                nullTerm = i;
                break;
            }

            if (nullTerm < 0 || nullTerm > c.Data.Length - 2)
                throw new InvalidDataException("Bad zTXt chunk: No terminator found.");

            _key = ChunkHelper.ToString(c.Data, 0, nullTerm);
            int compressionMethod = c.Data[nullTerm + 1];
            if (compressionMethod != 0)
                throw new InvalidDataException("Bad zTXt chunk: Unknown compression method.");

            byte[] uncomp = ChunkHelper.CompressBytes(c.Data, nullTerm + 2, c.Data.Length - nullTerm - 2, false); // uncompress
            _value = ChunkHelper.ToString(uncomp);
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkZTXT otherx = (PngChunkZTXT)other;
            _key = otherx._key;
            _value = otherx._value;
        }
    }
}
