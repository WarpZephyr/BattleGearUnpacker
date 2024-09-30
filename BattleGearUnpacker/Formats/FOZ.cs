using BattleGearUnpacker.Core.Compression;
using BattleGearUnpacker.Formats.Utility;
using BinaryMemory;

namespace BattleGearUnpacker.Formats
{
    /// <summary>
    /// Compressed data of some kind, purpose unknown.
    /// </summary>
    public class FOZ : FileFormat<FOZ>
    {
        #region Members

        /// <summary>
        /// The name of the underlying FON file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public int Unk10 { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public int Unk14 { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public int Unk18 { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public int Unk1C { get; set; }

        /// <summary>
        /// The underlying data.
        /// </summary>
        public byte[] Bytes { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="FOZ"/> with default settings.
        /// </summary>
        public FOZ()
        {
            Name = string.Empty;
            Unk10 = 1;
            Unk14 = 0;
            Unk18 = 0;
            Unk1C = 0;
            Bytes = [];
        }

        /// <summary>
        /// Create a new <see cref="FOZ"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the underlying FON file.</param>
        public FOZ(string name)
        {
            Name = name;
            Unk10 = 1;
            Unk14 = 0;
            Unk18 = 0;
            Unk1C = 0;
            Bytes = [];
        }

        #endregion

        #region Read

        /// <summary>
        /// Read a <see cref="FOZ"/> from a stream.
        /// </summary>
        /// <param name="br">The stream reader.</param>
        protected override void Read(BinaryStreamReader br)
        {
            br.BigEndian = false;
            Name = br.ReadUTF8(16);
            Unk10 = br.ReadInt32();
            Unk14 = br.ReadInt32();
            Unk18 = br.ReadInt32();
            Unk1C = br.ReadInt32();
            Bytes = Zlib.DecompressNext(br.BaseStream, (int)(br.Length - 32));
        }

        #endregion

        #region Write

        /// <summary>
        /// Write a <see cref="FOZ"/> to a stream.
        /// </summary>
        /// <param name="bw">The stream writer.</param>
        protected override void Write(BinaryStreamWriter bw)
        {
            bw.BigEndian = false;
            bw.WriteFixedUTF8(Name, 16, 0);
            bw.WriteInt32(Unk10);
            bw.WriteInt32(Unk14);
            bw.WriteInt32(Unk18);
            bw.WriteInt32(Unk1C);
            bw.WriteBytes(Zlib.Compress(Bytes));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Decompress a <see cref="FOZ"/> file and save the decompressed data as a file in the specified folder.
        /// </summary>
        /// <param name="path">The path to the <see cref="FOZ"/> file.</param>
        /// <param name="outFolder">The path to the folder to save the decompressed data to.</param>
        public static void DecompressTo(string path, string outFolder)
        {
            using var br = new BinaryStreamReader(path, false);
            string name = br.ReadUTF8(16);
            br.Position = 32;

            using var output = File.Create(Path.Combine(outFolder, name));
            Zlib.DecompressNext(br.BaseStream, output, br.BaseStream.Length - 32);
        }

        #endregion
    }
}
