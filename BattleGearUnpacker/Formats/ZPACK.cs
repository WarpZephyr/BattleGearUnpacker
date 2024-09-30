using BattleGearUnpacker.Core.Compression;
using BinaryMemory;

namespace BattleGearUnpacker.Formats
{
    /// <summary>
    /// A generic zlib archive with a file table in a separate header.
    /// </summary>
    public class ZPACK
    {
        #region Constants

        /// <summary>
        /// The size of each sector.
        /// </summary>
        internal const int SectorSize = 0x800;

        /// <summary>
        /// The allocated file entry count for <see cref="ZPACK"/> archives.
        /// </summary>
        public const int FileEntryCount = 8192;

        #endregion

        #region Members

        /// <summary>
        /// File entries in the archive, 8192 max.
        /// </summary>
        public List<FileEntry> FileEntries { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="ZPACK"/>.
        /// </summary>
        public ZPACK()
        {
            FileEntries = new List<FileEntry>(FileEntryCount);
        }

        #endregion

        #region Read

        /// <summary>
        /// Read a <see cref="ZPACK"/> from the specified paths.
        /// </summary>
        /// <param name="headerPath">The path to read the header from.</param>
        /// <param name="dataPath">The path to read the data from.</param>
        public static ZPACK Read(string headerPath, string dataPath)
        {
            using var headerReader = new BinaryStreamReader(headerPath, false);
            using var dataStream = File.OpenRead(dataPath);
            using var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        /// <summary>
        /// Read a <see cref="ZPACK"/> from the specified byte arrays.
        /// </summary>
        /// <param name="headerBytes">The bytes to read the header from.</param>
        /// <param name="dataBytes">The bytes to read the data from.</param>
        public static ZPACK Read(byte[] headerBytes, byte[] dataBytes)
        {
            using var headerReader = new BinaryStreamReader(headerBytes, false);
            using var dataStream = new MemoryStream(dataBytes, false);
            using var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        /// <summary>
        /// Read a <see cref="ZPACK"/> from the specified streams.
        /// </summary>
        /// <param name="headerStream">The <see cref="Stream"/> to read the header from.</param>
        /// <param name="dataStream">The <see cref="Stream"/> to read the data from.</param>
        public static ZPACK Read(Stream headerStream, Stream dataStream)
        {
            using var headerReader = new BinaryStreamReader(headerStream, false, true);
            using var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        #endregion

        #region Write

        /// <summary>
        /// Write this <see cref="ZPACK"/> to the specified paths.
        /// </summary>
        /// <param name="headerPath">The path to write the header to.</param>
        /// <param name="dataPath">The path to write the data to.</param>
        public void Write(string headerPath, string dataPath)
        {
            using var headerWriter = new BinaryStreamWriter(headerPath, false);
            using var dataStream = File.Create(dataPath);
            using var dataSectorStream = new SectorStream(dataStream, SectorSize);
            Write(headerWriter, dataSectorStream);
        }

        /// <summary>
        /// Write this <see cref="ZPACK"/> to the specified streams.
        /// </summary>
        /// <param name="headerStream">The <see cref="Stream"/> to write the header to.</param>
        /// <param name="dataStream">The <see cref="Stream"/> to write the data to.</param>
        public void Write(Stream headerStream, Stream dataStream)
        {
            using var headerWriter = new BinaryStreamWriter(headerStream, false, true);
            using var dataSectorStream = new SectorStream(dataStream, SectorSize);
            Write(headerWriter, dataSectorStream);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Read a <see cref="ZPACK"/> from a stream.
        /// </summary>
        internal static ZPACK Read(BinaryStreamReader headerReader, SectorStream dataStream)
        {
            var zpack = new ZPACK();

            headerReader.BigEndian = false;
            for (int i = 0; i < FileEntryCount; i++)
            {
                var entry = new FileEntry(headerReader, dataStream);
                if (entry.IsEmpty)
                    break;

                zpack.FileEntries.Add(entry);
            }

            return zpack;
        }

        /// <summary>
        /// Write this <see cref="ZPACK"/> to a stream.
        /// </summary>
        internal void Write(BinaryStreamWriter headerWriter, SectorStream dataStream)
        {
            headerWriter.BigEndian = false;
            for (int i = 0; i < FileEntries.Count; i++)
            {
                FileEntries[i].Write(headerWriter, dataStream);
            }

            int emptyCount = FileEntryCount - FileEntries.Count;
            for (int i = 0; i < emptyCount; i++)
            {
                headerWriter.WritePattern(40, 0);
            }
        }

        #endregion

        /// <summary>
        /// A file entry in a <see cref="ZPACK"/> archive.
        /// </summary>
        public class FileEntry
        {
            #region Members

            /// <summary>
            /// An uppercase file name without any folders.<br/>
            /// 18 characters max length?<br/>
            /// Including null terminator?
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Unknown, seems random.
            /// </summary>
            public short Unk10 { get; set; }

            /// <summary>
            /// The size of the uncompressed data.
            /// </summary>
            public int Size => Bytes != null ? Bytes.Length : throw new InvalidOperationException("Entry data is null.");

            /// <summary>
            /// Unknown, always -1.
            /// </summary>
            public int Unk24 { get; set; }

            /// <summary>
            /// The bytes of the file.
            /// </summary>
            public byte[]? Bytes { get; set; }

            /// <summary>
            /// Whether or not the entry is empty.
            /// </summary>
            public bool IsEmpty
                => Unk24 == 0;

            #endregion

            #region Constructors

            /// <summary>
            /// Create a new and empty <see cref="FileEntry"/>.
            /// </summary>
            public FileEntry()
            {
                Name = string.Empty;
                Unk10 = 0;
                Unk24 = -1;
            }

            /// <summary>
            /// Create a new and blank <see cref="FileEntry"/>
            /// </summary>
            /// <param name="name"></param>
            public FileEntry(string name)
            {
                Name = name;
                Unk10 = -1;
                Unk24 = -1;
            }

            /// <summary>
            /// Read a <see cref="FileEntry"/>.
            /// </summary>
            internal FileEntry(BinaryStreamReader headerReader, SectorStream dataStream)
            {
                Name = headerReader.ReadUTF8(18);
                Unk10 = headerReader.ReadInt16();
                int sectorOffset = headerReader.ReadInt32();
                headerReader.ReadInt32(); // Sector Count
                int compressedSize = headerReader.ReadInt32();
                int size = headerReader.ReadInt32();
                Unk24 = headerReader.ReadInt32();

                if (compressedSize > 0)
                {
                    dataStream.Position = sectorOffset;
                    Bytes = Zlib.DecompressNext(dataStream.BaseStream, compressedSize, size);
                }
            }

            #endregion

            #region Methods

            /// <summary>
            /// Write a <see cref="FileEntry"/>.
            /// </summary>
            /// <param name="headerWriter"></param>
            /// <param name="dataStream"></param>
            internal void Write(BinaryStreamWriter headerWriter, SectorStream dataStream)
            {
                headerWriter.WriteFixedUTF8(Name, 18, 0);
                headerWriter.WriteInt16(Unk10);

                // Write sector offset
                int sectorOffset = (int)dataStream.Position;
                headerWriter.WriteInt32(sectorOffset);

                // Write compressed bytes
                long compressStart = dataStream.BaseStream.Position;

                // Allow empty files
                if (Bytes != null)
                    Zlib.Compress(Bytes, dataStream);

                long compressEnd = dataStream.BaseStream.Position;
                int compressedSize = (int)(compressEnd - compressStart);

                // Ensure we are on next sector for count calculation
                dataStream.PadSector();

                // Write sector count
                headerWriter.WriteInt32((int)dataStream.Position - sectorOffset);
                headerWriter.WriteInt32(compressedSize);
                headerWriter.WriteInt32(Bytes != null ? Bytes.Length : 0);
                headerWriter.WriteInt32(Unk24);
            }

            #endregion
        }
    }
}
