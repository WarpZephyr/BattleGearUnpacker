using BinaryMemory;
using System.Diagnostics.CodeAnalysis;

namespace BattleGearUnpacker.Formats.Utility
{
    public abstract class FileFormat<TFormat> where TFormat : FileFormat<TFormat>, new()
    {
        #region Is

        protected virtual bool Is(BinaryStreamReader br)
            => throw new NotImplementedException($"{nameof(Is)} is not implemented for this format.");

        public static bool Is(string path)
        {
            using var br = new BinaryStreamReader(path);
            return br.Length > 0 && new TFormat().Is(br);
        }

        public static bool Is(byte[] bytes)
        {
            if (bytes.Length < 0)
                return false;

            using var br = new BinaryStreamReader(bytes);
            return new TFormat().Is(br);
        }

        public static bool Is(Stream stream)
        {
            if (stream.Length < 0)
                return false;

            long pos = stream.Position;
            using var br = new BinaryStreamReader(stream, false, true);
            bool result = new TFormat().Is(br);
            stream.Position = pos;
            return result;
        }

        #endregion

        #region Read

        protected virtual void Read(BinaryStreamReader br)
            => throw new NotImplementedException($"{nameof(Read)} is not implemented for this format.");

        public static TFormat Read(string path)
        {
            using var br = new BinaryStreamReader(path);
            var format = new TFormat();
            format.Read(br);
            return format;
        }

        public static TFormat Read(byte[] bytes)
        {
            using var br = new BinaryStreamReader(bytes);
            var format = new TFormat();
            format.Read(br);
            return format;
        }

        public static TFormat Read(Stream stream)
        {
            using var br = new BinaryStreamReader(stream, false, true);
            var format = new TFormat();
            format.Read(br);
            return format;
        }

        #endregion

        #region IsRead

        private static bool IsRead(BinaryStreamReader br, [NotNullWhen(true)] out TFormat? format)
        {
            long start = br.Position;
            format = new TFormat();
            if (format.Is(br))
            {
                br.Position = start;
                format.Read(br);
                return true;
            }

            br.Position = start;
            format = null;
            return false;
        }

        public static bool IsRead(string path, [NotNullWhen(true)] out TFormat? format)
        {
            using var br = new BinaryStreamReader(path);
            return IsRead(br, out format);
        }

        public static bool IsRead(byte[] bytes, [NotNullWhen(true)] out TFormat? format)
        {
            if (bytes.Length == 0)
            {
                format = null;
                return false;
            }

            using var br = new BinaryStreamReader(bytes);
            return IsRead(br, out format);
        }

        public static bool IsRead(Stream stream, [NotNullWhen(true)] out TFormat? format)
        {
            if (stream.Length == 0)
            {
                format = null;
                return false;
            }

            using var br = new BinaryStreamReader(stream, false, true);
            return IsRead(br, out format);
        }

        #endregion

        #region Write

        protected virtual void Write(BinaryStreamWriter bw)
            => throw new NotImplementedException($"{nameof(Write)} is not implemented for this format.");

        public void Write(string path)
        {
            using var bw = new BinaryStreamWriter(path);
            Write(bw);
        }

        public void Write(byte[] bytes)
        {
            using var bw = new BinaryStreamWriter(bytes);
            Write(bw);
        }

        public void Write(Stream stream)
        {
            using var bw = new BinaryStreamWriter(stream, false, true);
            Write(bw);
        }

        public byte[] Write()
        {
            using var bw = new BinaryStreamWriter();
            Write(bw);
            return bw.ToArray();
        }

        #endregion
    }
}
