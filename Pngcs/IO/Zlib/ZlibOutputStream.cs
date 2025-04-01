using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;

namespace Pngcs.IO.Zlib
{
    /// <summary>
    /// Zlib output (deflater) based on SharpZipLib.
    /// </summary>
    internal class ZlibOutputStream : Stream
    {
        readonly protected Stream _baseStream;
        readonly protected bool _leaveOpen;
        protected int _compressLevel;
        protected DeflateCompressStrategy _strategy;

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override long Length
            => throw new NotImplementedException();

        public override bool CanRead => false;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override bool CanTimeout => false;

        private readonly DeflaterOutputStream _deflaterOutputStream;
        private readonly Deflater _deflater;

        public ZlibOutputStream(Stream stream, int compressLevel, DeflateCompressStrategy strategy, bool leaveOpen)
        {
            _baseStream = stream;
            _leaveOpen = leaveOpen;
            _strategy = strategy;
            _compressLevel = compressLevel;

            _deflater = new Deflater(compressLevel);
            SetStrategy(strategy);
            _deflaterOutputStream = new DeflaterOutputStream(stream, _deflater)
            {
                IsStreamOwner = !leaveOpen
            };
        }

        public override void SetLength(long value)
            => throw new NotImplementedException();


        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotImplementedException();


        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Cannot read in an output stream.");

        public void SetStrategy(DeflateCompressStrategy strategy)
        {
            switch (strategy)
            {
                case DeflateCompressStrategy.Filtered:
                    _deflater.SetStrategy(DeflateStrategy.Filtered);
                    break;
                case DeflateCompressStrategy.Huffman:
                    _deflater.SetStrategy(DeflateStrategy.HuffmanOnly);
                    break;
                case DeflateCompressStrategy.Default:
                    _deflater.SetStrategy(DeflateStrategy.Default);
                    break;
                default:
                    throw new NotSupportedException($"Unknown {nameof(DeflateCompressStrategy)}: {strategy}");
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
            => _deflaterOutputStream.Write(buffer, offset, count);

        public override void WriteByte(byte value)
            => _deflaterOutputStream.WriteByte(value);


        public override void Close()
            => _deflaterOutputStream.Close();


        public override void Flush()
            => _deflaterOutputStream.Flush();
    }
}
