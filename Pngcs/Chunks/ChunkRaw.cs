using Pngcs.Helpers;
using Pngcs.IO.Hashing;
using System;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Wraps raw chunk data.
    /// </summary>
    /// <remarks>
    /// Short lived object, to be created while serialing/deserializing.<br/>
    /// Do not reuse it for different chunks.<br/>
    /// http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
    ///</remarks>
    public class ChunkRaw
    {
        /// <summary>
        /// The length counts only the data field, not itself, the chunk type code, or the CRC.<br/>
        /// Zero is a valid length.<br/>
        /// <br/>
        /// Although encoders and decoders should treat the length as unsigned, its value must not exceed 2^31-1 bytes.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Chunk Id, as array of 4 bytes
        /// </summary>
        public readonly byte[] IdBytes;

        /// <summary>
        /// Raw data, crc not included
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// The CRC value of the chunk.
        /// </summary>
        private int crcValue;

        /// <summary>
        /// Creates an empty raw chunk.
        /// </summary>
        /// <param name="length">The length of data within the chunk.</param>
        /// <param name="idbytes">The ID of the chunk.</param>
        /// <param name="alloc">Whether or not to pre-allocate the data buffer.</param>
        internal ChunkRaw(int length, byte[] idbytes, bool alloc)
        {
            IdBytes = new byte[4];
            crcValue = 0;
            Length = length;
            Array.Copy(idbytes, 0, IdBytes, 0, 4);
            if (alloc)
                Data = new byte[Length];
            else
                Data = [];
        }

        /// <summary>
        /// Called after setting data, before writing to output.
        /// </summary>
        private int ComputeCrc()
        {
            CRC32 crcengine = PngHelperInternal.GetCRC();
            crcengine.Reset();
            crcengine.Update(IdBytes, 0, 4);
            if (Length > 0)
                crcengine.Update(Data, 0, Length);
            return (int)crcengine.GetValue();
        }

        /// <summary>
        /// Writes this chunk to an output <see cref="Stream"/>.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <exception cref="InvalidDataException">The chunk ID was invalid.</exception>
        internal void WriteChunk(Stream output)
        {
            if (IdBytes.Length != 4)
                throw new InvalidDataException($"Bad chunk ID: {ChunkHelper.ToString(IdBytes)}");

            crcValue = ComputeCrc();
            PngHelperInternal.WriteInt32(output, Length);
            PngHelperInternal.WriteBytes(output, IdBytes);
            if (Length > 0)
                PngHelperInternal.WriteBytes(output, Data, 0, Length);

            PngHelperInternal.WriteInt32(output, crcValue);
        }

        /// <summary>
        /// Position before: just after chunk id. positon after: after crc Data should
        /// be already allocated. Checks CRC Return number of byte read.
        /// </summary>
        internal int ReadChunkData(Stream stream, bool checkCrc)
        {
            PngHelperInternal.ReadBytes(stream, Data, 0, Length);
            crcValue = PngHelperInternal.ReadInt32(stream);
            if (checkCrc)
            {
                int crc = ComputeCrc();
                if (crc != crcValue)
                    throw new InvalidDataException($"Invalid CRC for chunk {this} Expected: {crc}, Read: {crcValue}");
            }
            return Length + 4;
        }

        /// <summary>
        /// Gets the data as a new <see cref="MemoryStream"/>.
        /// </summary>
        /// <returns>A new <see cref="MemoryStream"/>.</returns>
        internal MemoryStream ToMemoryStream()
            => new(Data);

        /// <summary>
        /// The ID and length of this chunk.
        /// </summary>
        /// <returns>The ID and length of this chunk</returns>
        public override string ToString()
            => $"ChunkID={ChunkHelper.ToString(IdBytes)}, Length={Length}";
    }
}
