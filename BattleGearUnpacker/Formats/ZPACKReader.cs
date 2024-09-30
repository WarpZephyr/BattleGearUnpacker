using BattleGearUnpacker.Core.Compression;
using BinaryMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BattleGearUnpacker.Formats
{
    /// <summary>
    /// An on-demand reader for <see cref="ZPACK"/> archives.
    /// </summary>
    public class ZPACKReader : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The size of each sector.
        /// </summary>
        internal const int SectorSize = 0x800;

        /// <summary>
        /// The allocated file entry count for <see cref="ZPACK"/> archives.
        /// </summary>
        public const int FileEntryCount = 8192;

        /// <summary>
        /// The data <see cref="Stream"/>.
        /// </summary>
        private readonly SectorStream _dataStream;

        /// <summary>
        /// File entry headers in the archive, 8192 count.
        /// </summary>
        public List<FileEntry> FileEntries { get; private set; }

        /// <summary>
        /// Whether or not the reader's data stream has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Create a new <see cref="ZPACKReader"/>.
        /// </summary>
        /// <param name="dataStream">The data stream to read from.</param>
        private ZPACKReader(SectorStream dataStream)
        {
            _dataStream = dataStream;
            FileEntries = new List<FileEntry>(FileEntryCount);
        }

        #region Read

        /// <summary>
        /// Create a <see cref="ZPACKReader"/> from the specified paths.
        /// </summary>
        /// <param name="headerPath">The path to read the header from.</param>
        /// <param name="dataPath">The path to read data from.</param>
        public static ZPACKReader Read(string headerPath, string dataPath)
        {
            using var headerReader = new BinaryStreamReader(headerPath, false);
            var dataStream = File.OpenRead(dataPath);
            var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        /// <summary>
        /// Create a <see cref="ZPACKReader"/> from the specified byte arrays.
        /// </summary>
        /// <param name="headerBytes">The bytes to read the header from.</param>
        /// <param name="dataBytes">The bytes to read data from.</param>
        public static ZPACKReader Read(byte[] headerBytes, byte[] dataBytes)
        {
            using var headerReader = new BinaryStreamReader(headerBytes, false);
            var dataStream = new MemoryStream(dataBytes, false);
            var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        /// <summary>
        /// Create a <see cref="ZPACKReader"/> from the specified streams.
        /// </summary>
        /// <param name="headerStream">The <see cref="Stream"/> to read the header from.</param>
        /// <param name="dataStream">The <see cref="Stream"/> to read data from.</param>
        public static ZPACKReader Read(Stream headerStream, Stream dataStream)
        {
            using var headerReader = new BinaryStreamReader(headerStream, false, true);
            var dataSectorStream = new SectorStream(dataStream, SectorSize);
            return Read(headerReader, dataSectorStream);
        }

        /// <summary>
        /// Create a <see cref="ZPACKReader"/> and read the header.
        /// </summary>
        internal static ZPACKReader Read(BinaryStreamReader headerReader, SectorStream dataStream)
        {
            var reader = new ZPACKReader(dataStream);
            headerReader.BigEndian = false;
            for (int i = 0; i < FileEntryCount; i++)
            {
                var entry = new FileEntry(headerReader, dataStream);
                if (entry.IsEmpty)
                    break;

                reader.FileEntries.Add(entry);
            }

            headerReader.Dispose();
            return reader;
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Disposes of the underlying data stream and enables <see cref="IsDisposed"/>
        /// </summary>
        /// <param name="disposing">Whether or not to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _dataStream.Dispose();
                }

                IsDisposed = true;
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _dataStream.DisposeAsync().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// A file entry header in a <see cref="ZPACK"/> archive.
        /// </summary>
        public class FileEntry
        {
            #region Members

            /// <summary>
            /// The data <see cref="Stream"/>.
            /// </summary>
            private readonly SectorStream _dataStream;

            /// <summary>
            /// An uppercase file name without any folders.<br/>
            /// 18 characters max length?<br/>
            /// Including null terminator?
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Unknown, seems random.
            /// </summary>
            public short Unk10 { get; private set; }

            /// <summary>
            /// The sector offset of the data.
            /// </summary>
            private int SectorOffset { get; set; }

            /// <summary>
            /// The amount of sectors the data occupies.
            /// </summary>
            private int SectorCount { get; set; }

            /// <summary>
            /// The size of the compressed data.
            /// </summary>
            private int CompressedSize { get; set; }

            /// <summary>
            /// The size of the uncompressed data.
            /// </summary>
            public int Size { get; private set; }

            /// <summary>
            /// Unknown, always -1.
            /// </summary>
            public int Unk24 { get; private set; }

            /// <summary>
            /// Whether or not the entry is empty.
            /// </summary>
            public bool IsEmpty
                => Unk24 == 0;

            #endregion

            /// <summary>
            /// Read a <see cref="FileEntry"/>.
            /// </summary>
            internal FileEntry(BinaryStreamReader headerReader, SectorStream dataStream)
            {
                _dataStream = dataStream;

                Name = headerReader.ReadUTF8(18);
                Unk10 = headerReader.ReadInt16();
                SectorOffset = headerReader.ReadInt32();
                SectorCount = headerReader.ReadInt32();
                CompressedSize = headerReader.ReadInt32();
                Size = headerReader.ReadInt32();
                Unk24 = headerReader.ReadInt32();
            }

            /// <summary>
            /// Decompress and read the underlying data of this entry.
            /// </summary>
            /// <returns>The data of this entry.</returns>
            public byte[] GetBytes()
            {
                _dataStream.Position = SectorOffset;
                return Zlib.DecompressNext(_dataStream.BaseStream, CompressedSize, Size);
            }

            public void GetStream(Stream output)
            {
                _dataStream.Position = SectorOffset;
                Zlib.DecompressNext(_dataStream.BaseStream, output, CompressedSize);
            }
        }
    }
}
