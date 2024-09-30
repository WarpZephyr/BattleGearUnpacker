using BinaryMemory;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.IO;
using System.Runtime.CompilerServices;

namespace BattleGearUnpacker.Core.Compression
{
    /// <summary>
    /// A static class containing Zlib helper functions.
    /// </summary>
    internal static class Zlib
    {
        #region Decompress

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes at the current position in <paramref name="input"/> with <paramref name="length"/> expected bytes.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        /// <param name="length">The expected decompressed length.</param>
        /// <returns>The decompressed data as an array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecompressNext(Stream input, int compressedLength, int length)
            => DecompressSlice(input, input.Position, compressedLength, length);

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes at the current position in <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        /// <returns>The decompressed data as an array of bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] DecompressNext(Stream input, int compressedLength)
            => DecompressSlice(input, input.Position, compressedLength);

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes at the current position in <paramref name="input"/> into <paramref name="output"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="output">The <see cref="Stream"/> to copy decompressed data to.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecompressNext(Stream input, Stream output, long compressedLength)
            => DecompressSlice(input, output, input.Position, compressedLength);

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes starting from <paramref name="position"/> in <paramref name="input"/> with <paramref name="length"/> expected bytes.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="position">The position to begin decompressing from.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        /// <param name="length">The expected decompressed length.</param>
        /// <returns>The decompressed data as an array of bytes.</returns>
        public static byte[] DecompressSlice(Stream input, long position, int compressedLength, int length)
        {
            using var substream = new SubStream(input, position, compressedLength);
            return Decompress(substream, length);
        }

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes starting from <paramref name="position"/> in <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="position">The position to begin decompressing from.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        /// <returns>The decompressed data as an array of bytes.</returns>
        public static byte[] DecompressSlice(Stream input, long position, int compressedLength)
        {
            using var substream = new SubStream(input, position, compressedLength);
            return Decompress(substream);
        }

        /// <summary>
        /// Decompress <paramref name="compressedLength"/> bytes starting from <paramref name="position"/> in <paramref name="input"/> into <paramref name="output"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress from.</param>
        /// <param name="output">The <see cref="Stream"/> to copy decompressed data to.</param>
        /// <param name="position">The position to begin decompressing from.</param>
        /// <param name="compressedLength">The length of the compressed data.</param>
        public static void DecompressSlice(Stream input, Stream output, long position, long compressedLength)
        {
            using var substream = new SubStream(input, position, compressedLength);
            Decompress(substream, output);
        }

        /// <summary>
        /// Decompress bytes from the specified input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress.</param>
        /// <param name="size">The length of the decompressed data.</param>
        /// <returns>Decompressed bytes.</returns>
        public static byte[] Decompress(Stream input, int size)
        {
            using var inflaterStream = new InflaterInputStream(input);
            inflaterStream.IsStreamOwner = false;

            byte[] bytes = new byte[size];
            inflaterStream.Read(bytes, 0, size);
            return bytes;
        }

        /// <summary>
        /// Decompress bytes from the specified input <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress.</param>
        /// <returns>Decompressed bytes.</returns>
        public static byte[] Decompress(Stream input)
        {
            using var inflaterStream = new InflaterInputStream(input);
            inflaterStream.IsStreamOwner = false;

            using var output = new MemoryStream();
            inflaterStream.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>
        /// Decompress into the specified output <see cref="Stream"/> from the specified input stream.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to decompress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <returns>A decompressed stream.</returns>
        public static void Decompress(Stream input, Stream output)
        {
            using var inflaterStream = new InflaterInputStream(input);
            inflaterStream.IsStreamOwner = false;
            inflaterStream.CopyTo(output);
        }

        #endregion

        #region Compress

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <returns>A compressed byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Compress(byte[] input)
            => Compress(input, Deflater.DEFAULT_COMPRESSION, false);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        /// <returns>A compressed byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Compress(byte[] input, int level)
            => Compress(input, level, false);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        /// <returns>A compressed byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Compress(byte[] input, bool noHeaderOrFooter)
            => Compress(input, Deflater.DEFAULT_COMPRESSION, noHeaderOrFooter);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        /// <returns>A compressed byte array.</returns>
        public static byte[] Compress(byte[] input, int level, bool noHeaderOrFooter)
        {
            using var output = new MemoryStream();
            Compress(input, output, level, noHeaderOrFooter);
            return output.ToArray();
        }

        /// <summary>
        /// Compress a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Compress(Stream input, Stream output)
            => Compress(input, output, Deflater.DEFAULT_COMPRESSION, false);

        /// <summary>
        /// Compress a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Compress(Stream input, Stream output, int level)
            => Compress(input, output, level, false);

        /// <summary>
        /// Compress a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Compress(Stream input, Stream output, bool noHeaderOrFooter)
            => Compress(input, output, Deflater.DEFAULT_COMPRESSION, noHeaderOrFooter);

        /// <summary>
        /// Compress a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">The <see cref="Stream"/> to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        public static void Compress(Stream input, Stream output, int level, bool noHeaderOrFooter)
        {
            var deflater = new Deflater(level, noHeaderOrFooter);
            using var deflateStream = new DeflaterOutputStream(output, deflater);
            deflateStream.IsStreamOwner = false;
            input.CopyTo(deflateStream);
            deflateStream.Flush();
            deflateStream.Finish();
        }

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        public static void Compress(byte[] input, Stream output)
            => Compress(input, output, Deflater.DEFAULT_COMPRESSION, false);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Compress(byte[] input, Stream output, int level)
            => Compress(input, output, level, false);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Compress(byte[] input, Stream output, bool noHeaderOrFooter)
            => Compress(input, output, Deflater.DEFAULT_COMPRESSION, noHeaderOrFooter);

        /// <summary>
        /// Compress a byte array.
        /// </summary>
        /// <param name="input">The bytes to compress.</param>
        /// <param name="output">The output <see cref="Stream"/>.</param>
        /// <param name="level">The level of compression, from 0 to 9.</param>
        /// <param name="noHeaderOrFooter">Whether or not to exclude writing the header and footer.</param>
        public static void Compress(byte[] input, Stream output, int level, bool noHeaderOrFooter)
        {
            using var deflateStream = new DeflaterOutputStream(output, new Deflater(level, noHeaderOrFooter));
            deflateStream.IsStreamOwner = false;
            deflateStream.Write(input, 0, input.Length);
            deflateStream.Flush();
            deflateStream.Finish();
        }

        #endregion
    }
}
