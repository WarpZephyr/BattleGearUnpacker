using Pngcs.Helpers;
using Pngcs.IO.Hashing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    /// Reads IDAT chunks
    /// </summary>
    internal class PngIDatChunkInputStream : Stream
    {
        // Purely informational.
        public class IdatChunkInfo
        {
            public readonly int _length;
            public readonly long _offset;

            public IdatChunkInfo(int length, long offset)
            {
                _length = length;
                _offset = offset;
            }
        }

        private readonly Stream _inputStream;
        private readonly CRC32 crcEngine;
        public bool CheckCrc { get; set; }
        public int LastChunkLength { get; private set; }
        public byte[] LastChunkID { get; private set; }

        private int _toReadThisChunk;
        private bool ended;
        private long _offset; // offset inside inputstream

        public override long Position { get; set; }
        public override long Length => 0;
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanSeek => false;

        public IList<IdatChunkInfo> foundChunksInfo;

        /// <summary>
        /// Constructor must be called just after reading length and id of first IDAT chunk.
        /// </summary>
        public PngIDatChunkInputStream(Stream iStream, int lenFirstChunk, long offset_0)
        {
            LastChunkID = new byte[4];
            _toReadThisChunk = 0;
            ended = false;
            foundChunksInfo = new List<IdatChunkInfo>();
            _offset = offset_0;
            CheckCrc = true;
            _inputStream = iStream;
            crcEngine = new CRC32();
            LastChunkLength = lenFirstChunk;
            _toReadThisChunk = lenFirstChunk;
            // we know it's a IDAT
            Array.Copy(Chunks.ChunkHelper.IDAT_BYTES, 0, LastChunkID, 0, 4);
            crcEngine.Update(LastChunkID, 0, 4);
            foundChunksInfo.Add(new IdatChunkInfo(LastChunkLength, offset_0 - 8));
            // PngHelper.logdebug("IDAT Initial fragment: len=" + lenLastChunk);
            if (LastChunkLength == 0)
                EndChunkGoForNext(); // rare, but...
        }

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => -1;
        public override void Flush() { }

        /// <summary>
        /// Does not close the base stream.
        /// </summary>
        public override void Close()
            => Close();

        private void EndChunkGoForNext()
        {
            // Called after readging the last byte of chunk
            // Checks CRC, and read ID from next CHUNK
            // Those values are left in idLastChunk / lenLastChunk
            // Skips empty IDATS
            do
            {
                int crc = PngHelperInternal.ReadInt32(_inputStream); //
                _offset += 4;
                if (CheckCrc)
                {
                    int crccalc = (int)crcEngine.GetValue();
                    if (LastChunkLength > 0 && crc != crccalc)
                        throw new InvalidDataException($"Invalid CRC for IDAT, offset: {_offset}");
                    crcEngine.Reset();
                }

                LastChunkLength = PngHelperInternal.ReadInt32(_inputStream);
                if (LastChunkLength < 0)
                    throw new InvalidDataException($"Invalid length for chunk: {LastChunkLength}");
                _toReadThisChunk = LastChunkLength;
                PngHelperInternal.ReadBytes(_inputStream, LastChunkID, 0, 4);
                _offset += 8;

                ended = !ArrayHelper.ArraysEqual4(LastChunkID, Chunks.ChunkHelper.IDAT_BYTES);

                if (!ended)
                {
                    foundChunksInfo.Add(new IdatChunkInfo(LastChunkLength, _offset - 8));
                    if (CheckCrc)
                        crcEngine.Update(LastChunkID, 0, 4);
                }
                // PngHelper.logdebug("IDAT ended. next len= " + lenLastChunk + " idat?" +
                // (!ended));
            } while (LastChunkLength == 0 && !ended);
            // rarely condition is true (empty IDAT ??)
        }

        /// <summary>
        /// Sometimes last row read does not fully consumes the chunk here we read the remaining dummy bytes.
        /// </summary>
        public void ForceChunkEnd()
        {
            if (!ended)
            {
                byte[] dummy = new byte[_toReadThisChunk];
                PngHelperInternal.ReadBytes(_inputStream, dummy, 0, _toReadThisChunk);
                if (CheckCrc)
                    crcEngine.Update(dummy, 0, _toReadThisChunk);
                EndChunkGoForNext();
            }
        }

        /// <summary>
        /// This can return less than len, but never 0 Returns -1 nothing more to read, -2 if "pseudo file" ended prematurely. That is our error.
        /// </summary>
        public override int Read(byte[] b, int off, int len_0)
        {
            // Can happen only when raw reading, see PngReader.ReadAndSkipsAllRows()
            if (ended)
                return -1;

            if (_toReadThisChunk <= 0)
                throw new Exception($"{nameof(_toReadThisChunk)} was less than or equal to {0}, this should not happen.");

            int readCount = _inputStream.Read(b, off, len_0 >= _toReadThisChunk ? _toReadThisChunk : len_0);
            if (readCount == -1)
                readCount = -2;

            if (readCount > 0)
            {
                if (CheckCrc)
                    crcEngine.Update(b, off, readCount);

                _offset += readCount;
                _toReadThisChunk -= readCount;
            }
            if (readCount >= 0 && _toReadThisChunk == 0)
            {
                // End of chunk, prepare for the next one.
                EndChunkGoForNext();
            }

            return readCount;
        }

        public int Read(byte[] b)
            => Read(b, 0, b.Length);

        public override int ReadByte()
        {
            // Inefficient, but this should be used rarely.
            byte[] b1 = new byte[1];
            int r = Read(b1, 0, 1);
            return r < 0 ? -1 : b1[0];
        }

        public int GetLastChunkLength()
            => LastChunkLength;

        public byte[] GetLastChunkID()
            => LastChunkID;

        public long GetOffset()
            => _offset;

        public bool IsEnded()
            => ended;

        /// <summary>
        /// Disables CRC checking. This can make reading faster
        /// </summary>
        internal void DisableCrcCheck()
        {
            CheckCrc = false;
        }
    }
}
