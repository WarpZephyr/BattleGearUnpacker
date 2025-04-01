using System;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// iTXt chunk:  http://www.w3.org/TR/PNG/#11iTXt
    /// One of the three text chunks
    /// </summary>
    public class PngChunkITXT : PngChunkTextVar
    {
        public const string ID = ChunkHelper.iTXt;

        public bool Compressed { get; set; } = false;
        public string LangTag { get; set; } = "";
        public string TranslatedTag { get; set; } = "";

        public PngChunkITXT(ImageInfo info)
            : base(ID, info) { }

        public override ChunkRaw CreateRawChunk()
        {
            if (_key.Length == 0)
                throw new InvalidOperationException("Text chunk key must be non-empty.");

            var ba = new MemoryStream();
            ChunkHelper.WriteBytes(ba, ChunkHelper.ToBytes(_key));
            ba.WriteByte(0); // separator
            ba.WriteByte(Compressed ? (byte)1 : (byte)0);
            ba.WriteByte(0); // compression method (always 0)
            ChunkHelper.WriteBytes(ba, ChunkHelper.ToBytes(LangTag));
            ba.WriteByte(0); // separator
            ChunkHelper.WriteBytes(ba, ChunkHelper.ToBytesUTF8(TranslatedTag));
            ba.WriteByte(0); // separator
            byte[] textbytes = ChunkHelper.ToBytesUTF8(_value);
            if (Compressed)
                textbytes = ChunkHelper.CompressBytes(textbytes, true);

            ChunkHelper.WriteBytes(ba, textbytes);
            byte[] b = ba.ToArray();
            ChunkRaw chunk = CreateEmptyChunk(b.Length, false);
            chunk.Data = b;
            return chunk;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            int nullsFound = 0;
            int[] nullsIdx = new int[3];
            for (int k = 0; k < c.Data.Length; k++)
            {
                if (c.Data[k] != 0)
                    continue;
                nullsIdx[nullsFound] = k;
                nullsFound++;
                if (nullsFound == 1)
                    k += 2;
                if (nullsFound == 3)
                    break;
            }
            if (nullsFound != 3)
                throw new InvalidDataException("Bad formed PngChunkITXT chunk.");

            _key = ChunkHelper.ToString(c.Data, 0, nullsIdx[0]);
            int i = nullsIdx[0] + 1;
            Compressed = c.Data[i] != 0;
            i++;

            if (Compressed && c.Data[i] != 0)
                throw new InvalidDataException("Bad formed PngChunkITXT chunk - bad compression method ");

            LangTag = ChunkHelper.ToString(c.Data, i, nullsIdx[1] - i);
            TranslatedTag = ChunkHelper.ToStringUTF8(c.Data, nullsIdx[1] + 1, nullsIdx[2] - nullsIdx[1] - 1);
            i = nullsIdx[2] + 1;
            if (Compressed)
            {
                byte[] bytes = ChunkHelper.CompressBytes(c.Data, i, c.Data.Length - i, false);
                _value = ChunkHelper.ToStringUTF8(bytes);
            }
            else
            {
                _value = ChunkHelper.ToStringUTF8(c.Data, i, c.Data.Length - i);
            }
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkITXT otherx = (PngChunkITXT)other;
            _key = otherx._key;
            _value = otherx._value;
            Compressed = otherx.Compressed;
            LangTag = otherx.LangTag;
            TranslatedTag = otherx.TranslatedTag;
        }
    }
}
