using BattleGearUnpacker.Core.Compression;
using BattleGearUnpacker.Formats.Utility;
using BinaryMemory;
using System.IO;

namespace BattleGearUnpacker.Formats
{
    /// <summary>
    /// A file format that is entirely compressed when stored, purpose unknown.
    /// </summary>
    public class GST : FileFormat<GST>
    {
        #region Members

        /// <summary>
        /// The raw decompressed data of the <see cref="GST"/>.
        /// </summary>
        public byte[] Bytes { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="GST"/>.
        /// </summary>
        public GST()
        {
            Bytes = [];
        }

        #endregion

        #region Read

        /// <summary>
        /// Read a <see cref="GST"/> from a stream.
        /// </summary>
        /// <param name="br">The stream reader.</param>
        protected override void Read(BinaryStreamReader br)
        {
            Bytes = Zlib.Decompress(br.BaseStream);
        }

        #endregion

        #region Write

        /// <summary>
        /// Write a <see cref="GST"/> to a stream.
        /// </summary>
        /// <param name="bw">The stream writer.</param>
        protected override void Write(BinaryStreamWriter bw)
        {
            Zlib.Compress(Bytes, bw.BaseStream);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Decompress a <see cref="GST"/> file and save the decompressed data as a file.
        /// </summary>
        /// <param name="path">The path to the <see cref="GST"/> file.</param>
        /// <param name="outPath">The path to save the decompressed data to.</param>
        public static void DecompressTo(string path, string outPath)
        {
            using var input = File.OpenRead(path);
            using var output = File.Create(outPath);
            Zlib.Decompress(input, output);
            output.Flush();
        }

        /// <summary>
        /// Compress the specified file and save the compressed data as a file.
        /// </summary>
        /// <param name="path">The path to the file to compress.</param>
        /// <param name="outPath">The path to save the compressed file to.</param>
        public static void CompressTo(string path, string outPath)
        {
            using var input = File.OpenRead(path);
            using var output = File.Create(outPath);
            Zlib.Compress(input, output);
            output.Flush();
        }

        #endregion
    }
}
