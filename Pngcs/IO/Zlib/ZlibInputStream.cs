using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;

namespace Pngcs.IO.Zlib
{
    /// <summary>
    /// Zip input (inflater) based on SharpZipLib.
    /// </summary>
    internal class ZlibInputStream : Stream
    {
        protected readonly Stream _baseStream;
        protected readonly bool _leaveOpen;

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override long Length
            => throw new NotImplementedException();

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => false;
        public override bool CanTimeout => false;

        private readonly InflaterInputStream _inflaterInputStream;

        public ZlibInputStream(Stream stream, bool leaveOpen)
        {
            _baseStream = stream;
            _leaveOpen = leaveOpen;

            _inflaterInputStream = new InflaterInputStream(stream)
            {
                IsStreamOwner = !leaveOpen
            };
        }

        public override void SetLength(long value)
            => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Cannot write in a input stream.");

        public override int Read(byte[] array, int offset, int count)
            => _inflaterInputStream.Read(array, offset, count);

        public override int ReadByte()
            => _inflaterInputStream.ReadByte();

        public override void Close()
            => _inflaterInputStream.Close();

        public override void Flush()
            => _inflaterInputStream.Flush();
    }
}
