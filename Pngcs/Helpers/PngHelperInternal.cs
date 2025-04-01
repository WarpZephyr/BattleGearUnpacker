using Pngcs.IO.Hashing;
using System;
using System.IO;
using System.Text;

namespace Pngcs.Helpers
{
    /// <summary>
    /// Some utility static methods for internal use.
    /// </summary>
    internal class PngHelperInternal
    {
        [ThreadStatic]
        private static CRC32? _crc32Engine;

        public static readonly byte[] PNG_ID_SIGNATURE = [256 - 119, 80, 78, 71, 13, 10, 26, 10]; // png magic
        public static Encoding CharsetLatin1 = Encoding.GetEncoding("ISO-8859-1"); // charset
        public static Encoding CharsetUTF8 = Encoding.GetEncoding("UTF-8"); // charset used for some chunks

        /// <summary>
        /// Thread-singleton crc engine.
        /// </summary>
        public static CRC32 GetCRC()
        {
            _crc32Engine ??= new CRC32();
            return _crc32Engine;
        }

        public static int DoubleToInt100000(double d)
            => (int)(d * 100000.0 + 0.5);

        public static double IntToDouble100000(int i)
            => i / 100000.0;

        public static void WriteInt16(Stream output, int value)
        {
            output.WriteByte((byte)(value >> 8 & 0xff));
            output.WriteByte((byte)(value & 0xff));
        }

        /// <summary>
        /// Reads a 4-byte integer.
        /// </summary>
        public static int ReadInt32(Stream input)
        {
            int b1 = input.ReadByte();
            int b2 = input.ReadByte();
            int b3 = input.ReadByte();
            int b4 = input.ReadByte();
            if (b1 == -1 || b2 == -1 || b3 == -1 || b4 == -1)
                return -1;

            return (b1 << 24) + (b2 << 16) + (b3 << 8) + b4;
        }

        public static int ReadByte(byte[] value, int offset)
            => value[offset];

        public static int ReadInt16(byte[] value, int offset)
            => (value[offset] & 0xff) << 16 | value[offset + 1] & 0xff;

        public static int ReadInt32(byte[] value, int offset)
            => (value[offset] & 0xff) << 24 | (value[offset + 1] & 0xff) << 16
            | (value[offset + 2] & 0xff) << 8 | value[offset + 3] & 0xff;

        public static void WriteInt16(int value, byte[] b, int offset)
        {
            b[offset] = (byte)(value >> 8 & 0xff);
            b[offset + 1] = (byte)(value & 0xff);
        }

        public static void WriteInt32(int value, byte[] b, int offset)
        {
            b[offset] = (byte)(value >> 24 & 0xff);
            b[offset + 1] = (byte)(value >> 16 & 0xff);
            b[offset + 2] = (byte)(value >> 8 & 0xff);
            b[offset + 3] = (byte)(value & 0xff);
        }

        public static void WriteInt32(Stream output, int value)
        {
            output.WriteByte((byte)(value >> 24 & 0xff));
            output.WriteByte((byte)(value >> 16 & 0xff));
            output.WriteByte((byte)(value >> 8 & 0xff));
            output.WriteByte((byte)(value & 0xff));
        }

        /// <summary>
        /// Guaranteed to read exactly length bytes or it will throw an error.
        /// </summary>
        public static void ReadBytes(Stream input, byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return;

            int read = 0;
            while (read < length)
            {
                int readCount = input.Read(buffer, offset + read, length - read);
                if (readCount < 1)
                    throw new IOException($"Error reading, Read: {readCount}, Expected: {length}");

                read += readCount;
            }
        }

        public static void SkipBytes(Stream input, int length)
        {
            byte[] buffer = new byte[8192 * 4];
            int read = length;
            int remain = length;

            while (remain > 0)
            {
                read = input.Read(buffer, 0, remain > buffer.Length ? buffer.Length : remain);
                if (read < 0)
                    throw new EndOfStreamException($"End of stream error in: {nameof(SkipBytes)}");

                remain -= read;
            }
        }

        public static void WriteBytes(Stream output, byte[] b)
            => output.Write(b, 0, b.Length);

        public static void WriteBytes(Stream output, byte[] buffer, int offset, int count)
            => output.Write(buffer, offset, count);

        public static int ReadByte(Stream input)
            => input.ReadByte();

        public static void WriteByte(Stream output, byte value)
            => output.WriteByte(value);

        public static int UnfilterRowPaeth(int r, int a, int b, int c)
            // a = left, b = above, c = upper left
            => r + FilterPaethPredictor(a, b, c) & 0xFF;

        public static int FilterPaethPredictor(int a, int b, int c)
        {
            // from http://www.libpng.org/pub/png/spec/1.2/PNG-Filters.html
            // a = left, b = above, c = upper left
            int p = a + b - c;// ; initial estimate
            int pa = p >= a ? p - a : a - p;
            int pb = p >= b ? p - b : b - p;
            int pc = p >= c ? p - c : c - p;
            // ; return nearest of a,b,c,
            // ; breaking ties in order a,b,c.
            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else
                return c;
        }
    }
}
