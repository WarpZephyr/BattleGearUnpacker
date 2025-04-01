using System.IO;

namespace Pngcs.IO.Zlib
{
    internal class ZlibStreamFactory
    {
        public static ZlibInputStream CreateZlibInputStream(Stream stream, bool leaveOpen)
        {
            return new ZlibInputStream(stream, leaveOpen);
        }

        public static ZlibInputStream CreateZlibInputStream(Stream stream)
            => CreateZlibInputStream(stream, false);

        public static ZlibOutputStream CreateZlibOutputStream(Stream stream, int compressLevel, DeflateCompressStrategy strategy, bool leaveOpen)
        {
            return new ZlibOutputStream(stream, compressLevel, strategy, leaveOpen);
        }

        public static ZlibOutputStream CreateZlibOutputStream(Stream stream)
            => CreateZlibOutputStream(stream, false);

        public static ZlibOutputStream CreateZlibOutputStream(Stream stream, bool leaveOpen)
            => CreateZlibOutputStream(stream, DeflateCompressLevel.DEFAULT, DeflateCompressStrategy.Default, leaveOpen);
    }
}
