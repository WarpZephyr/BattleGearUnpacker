using BattleGearUnpacker.Core.Compression;
using BinaryMemory;

namespace BattleGearUnpacker.Formats
{
    internal class ZPACKWriter : IDisposable, IAsyncDisposable
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
        /// The header writer.
        /// </summary>
        private readonly BinaryStreamWriter _headerWriter;

        /// <summary>
        /// The data <see cref="Stream"/>.
        /// </summary>
        private readonly SectorStream _dataStream;

        /// <summary>
        /// File entry headers in the archive, 8192 max.
        /// </summary>
        private List<FileEntry> FileEntries { get; set; }

        /// <summary>
        /// Whether or not the writer has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Whether or not the writer has finished writing.
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Whether or not to leave the header and data stream open after disposing.
        /// </summary>
        public bool LeaveOpen { get; set; }

        /// <summary>
        /// Create a new <see cref="ZPACKWriter"/>.
        /// </summary>
        /// <param name="headerStream">The stream to write the header to.</param>
        /// <param name="dataStream">The stream to write data to.</param>
        /// <param name="leaveOpen">Whether or not to leave the header and data stream open after disposing.</param>
        public ZPACKWriter(Stream headerStream, Stream dataStream, bool leaveOpen)
        {
            LeaveOpen = leaveOpen;
            _headerWriter = new BinaryStreamWriter(headerStream, false, leaveOpen);
            _dataStream = new SectorStream(dataStream, SectorSize);
            FileEntries = new List<FileEntry>(FileEntryCount);
        }

        #region Create

        /// <summary>
        /// Creates a new <see cref="ZPACKWriter"/> with the given paths.
        /// </summary>
        /// <param name="headerPath">The path to write the header to.</param>
        /// <param name="dataPath">The path to write the data to.</param>
        /// <returns>A new <see cref="ZPACKWriter"/>.</returns>
        public static ZPACKWriter Create(string headerPath, string dataPath)
        {
            var headerStream = File.Create(headerPath);
            var dataStream = File.Create(dataPath, 4096, FileOptions.SequentialScan);
            return new ZPACKWriter(headerStream, dataStream, false);
        }

        /// <summary>
        /// Creates a new <see cref="ZPACKWriter"/> with the given streams.<br/>
        /// Sets <see cref="LeaveOpen"/> to true, leaving stream disposal subject to the caller by default.
        /// </summary>
        /// <param name="headerStream">The stream to write the header to.</param>
        /// <param name="dataStream">The stream to write the data to.</param>
        /// <returns>A new <see cref="ZPACKWriter"/>.</returns>
        public static ZPACKWriter Create(Stream headerStream, Stream dataStream)
            => new ZPACKWriter(headerStream, dataStream, true);

        /// <summary>
        /// A factory method to create a new <see cref="FileEntry"/>.
        /// </summary>
        /// <param name="name">The name of the <see cref="FileEntry"/>.</param>
        /// <returns>A file entry.</returns>
        public static FileEntry CreateFileEntry(string name)
            => new FileEntry(name);

        #endregion

        #region Write

        /// <summary>
        /// Writes a file and fills the given <see cref="FileEntry"/>.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="entry">The <see cref="FileEntry"/> to fill.</param>
        /// <exception cref="InvalidOperationException">Cannot add more files than the supported amount.</exception>
        public void WriteFile(string path, FileEntry entry)
        {
            if (FileEntries.Count == FileEntryCount)
                throw new InvalidOperationException($"Cannot add more than {FileEntryCount} file entries.");

            using var fs = File.OpenRead(path);
            entry.WriteData(_dataStream, fs);
            FileEntries.Add(entry);
        }

        /// <summary>
        /// Writes a file with a path and given entry name.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="name">The name to give the entry.</param>
        /// <exception cref="InvalidOperationException">Cannot add more files than the supported amount.</exception>
        public void WriteFile(string path, string name)
        {
            if (FileEntries.Count == FileEntryCount)
                throw new InvalidOperationException($"Cannot add more than {FileEntryCount} file entries.");

            using var fs = File.OpenRead(path);
            var entry = new FileEntry(name);
            entry.WriteData(_dataStream, fs);
            FileEntries.Add(entry);
        }

        /// <summary>
        /// Writes a file using only a path.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <exception cref="InvalidOperationException">Cannot add more files than the supported amount.</exception>
        public void WriteFile(string path)
        {
            if (FileEntries.Count == FileEntryCount)
                throw new InvalidOperationException($"Cannot add more than {FileEntryCount} file entries.");

            using var fs = File.OpenRead(path);
            var entry = new FileEntry(Path.GetFileName(path).ToUpperInvariant());
            entry.WriteData(_dataStream, fs);
            FileEntries.Add(entry);
        }

        /// <summary>
        /// Writes a dummy <see cref="FileEntry"/> that has no size.
        /// </summary>
        /// <param name="name">The name of the dummy <see cref="FileEntry"/>.</param>
        /// <exception cref="InvalidOperationException">Cannot add more files than the supported amount.</exception>
        public void WriteDummy(string name)
        {
            if (FileEntries.Count == FileEntryCount)
                throw new InvalidOperationException($"Cannot add more than {FileEntryCount} file entries.");

            var entry = new FileEntry(name);
            entry.WriteDummy(_dataStream);
            FileEntries.Add(entry);
        }

        /// <summary>
        /// Writes a dummy <see cref="FileEntry"/> that has no size.
        /// </summary>
        /// <param name="name">The dummy <see cref="FileEntry"/> to write.</param>
        /// <exception cref="InvalidOperationException">Cannot add more files than the supported amount.</exception>
        public void WriteDummy(FileEntry entry)
        {
            if (FileEntries.Count == FileEntryCount)
                throw new InvalidOperationException($"Cannot add more than {FileEntryCount} file entries.");

            entry.WriteDummy(_dataStream);
            FileEntries.Add(entry);
        }

        /// <summary>
        /// Finishes writing.
        /// </summary>
        public void Finish()
        {
            int emptyCount = FileEntryCount - FileEntries.Count;
            foreach (var entry in FileEntries)
            {
                entry.WriteEntry(_headerWriter);
            }

            // Write empty entries
            for (int i = 0; i < emptyCount; i++)
            {
                _headerWriter.WritePattern(40, 0);
            }

            IsFinished = true;
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
                    if (!IsFinished)
                        Finish();

                    if (!LeaveOpen)
                    {
                        _headerWriter.Dispose();
                        _dataStream.Dispose();
                    }
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
            /// Creates a new <see cref="FileEntry"/>.
            /// </summary>
            /// <param name="name">The name of the entry.</param>
            public FileEntry(string name)
            {
                Name = name;
            }

            /// <summary>
            /// Writes this file entry to a stream for the header.
            /// </summary>
            /// <param name="bw">The stream writer.</param>
            internal void WriteEntry(BinaryStreamWriter bw)
            {
                bw.WriteFixedUTF8(Name, 18, 0);
                bw.WriteInt16(Unk10);
                bw.WriteInt32(SectorOffset);
                bw.WriteInt32(SectorCount);
                bw.WriteInt32(CompressedSize);
                bw.WriteInt32(Size);
                bw.WriteInt32(Unk24);
            }

            /// <summary>
            /// Writes data to a data stream and fills this file entry with information for the header.
            /// </summary>
            /// <param name="dataStream">The data stream to write to.</param>
            /// <param name="file">The file to write.</param>
            internal void WriteData(SectorStream dataStream, Stream file)
            {
                SectorOffset = (int)dataStream.Position;
                Size = (int)file.Length;
                Unk24 = -1;

                // Write compressed bytes
                long compressStart = dataStream.BaseStream.Position;
                Zlib.Compress(file, dataStream.BaseStream);
                long compressEnd = dataStream.BaseStream.Position;
                CompressedSize = (int)(compressEnd - compressStart);

                // Ensure we are on next sector for count calculation
                dataStream.PadSector();
                SectorCount = (int)dataStream.Position - SectorOffset;
            }

            /// <summary>
            /// Fills this file entry with information for a dummy entry without data.
            /// </summary>
            /// <param name="dataStream">The data stream to get the position of.</param>
            internal void WriteDummy(SectorStream dataStream)
            {
                SectorOffset = (int)dataStream.Position;
                SectorCount = 0;
                CompressedSize = 0;
                Size = 0;
                Unk24 = -1;
            }
        }
    }
}
