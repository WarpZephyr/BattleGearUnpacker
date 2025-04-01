using Pngcs.Chunks;
using Pngcs.Drawing;
using Pngcs.Helpers;
using Pngcs.IO.Zlib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    /// Reads a PNG image, line by line
    /// </summary>
    /// <remarks>
    /// The typical reading sequence is as follows:<br/>
    /// <br/>
    /// 1. At construction time, the header and IHDR chunk are read (basic image info)<br/>
    /// <br/>
    /// 2  (Optional) you can set some global options: UnpackedMode CrcCheckDisabled<br/>
    /// <br/>
    /// 3. (Optional) If you call GetMetadata() or or GetChunksLisk() before reading the pixels, the chunks before IDAT are automatically loaded and available<br/>
    /// <br/>
    /// 4a. The rows are read, one by one, with the <tt>ReadRowXXX</tt> methods: (ReadRowInt() , ReadRowByte(), etc)<br/>
    /// in order, from 0 to nrows-1 (you can skip or repeat rows, but not go backwards)<br/>
    /// <br/>
    /// 4b. Alternatively, you can read all rows, or a subset, in a single call: see ReadRowsInt(), ReadRowsByte()<br/>
    /// In general this consumes more memory, but for interlaced images this is equally efficient, and more so if reading a small subset of rows.<br/>
    ///<br/>
    /// 5. Read of the last row automatically loads the trailing chunks, and ends the reader.<br/>
    /// <br/>
    /// 6. End() forcibly finishes/aborts the reading and closes the stream<br/>
    /// </remarks>
    public class PngReader
    {
        /// <summary>
        /// The compression stream.
        /// </summary>
        internal ZlibInputStream _idatInputstream;

        /// <summary>
        /// The pixel data stream.
        /// </summary>
        internal PngIDatChunkInputStream _iIdatCstream;

        /// <summary>
        /// The input stream.
        /// </summary>
        private readonly Stream _baseStream;

        /// <summary>
        /// A deinterlacer.
        /// </summary>
        private readonly PngDeinterlacer? _deinterlacer;

        /// <summary>
        /// The chunks to skip, lazily created.
        /// </summary>
        private Dictionary<string, int>? _skipChunkIdsSet; // lazily created

        /// <summary>
        /// A high level wrapper of a ChunksList, or a list of read chunks.
        /// </summary>
        private readonly PngMetadata _metadata;

        /// <summary>
        /// Read chunks.
        /// </summary>
        private readonly ChunksList _chunksList;

        /// <summary>
        /// Cache for commonly re-accessed chunk data in read helpers.
        /// </summary>
        private Color[]? _plte;

        /// <summary>
        /// Cache for commonly re-accessed chunk data in read helpers.
        /// </summary>
        private L8 _trnsGray8;

        /// <summary>
        /// Cache for commonly re-accessed chunk data in read helpers.
        /// </summary>
        private L16 _trnsGray16;

        /// <summary>
        /// Cache for commonly re-accessed chunk data in read helpers.
        /// </summary>
        private Rgb24 _trnsRgb24;

        /// <summary>
        /// Cache for commonly re-accessed chunk data in read helpers.
        /// </summary>
        private Rgb48 _trnsRgb48;

        /// <summary>
        /// The state of the chunk cache.<br/>
        /// -1 means not searched.<br/>
        /// 0 means searched but not found.<br/>
        /// 1 means found.
        /// </summary>
        private int _plteCacheState = -1;

        /// <summary>
        /// The state of the chunk cache.<br/>
        /// -1 means not searched.<br/>
        /// 0 means searched but not found.<br/>
        /// 1 means found.
        /// </summary>
        private int _trnsCacheState = -1;

        /// <summary>
        /// A buffer for the last read image line.
        /// </summary>
        protected ImageLine? _imageLine;

        /// <summary>
        /// Basic image info, inmutable.
        /// </summary>
        public ImageInfo ImgInfo { get; private set; }

        /// <summary>
        /// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
        /// </summary>
        protected byte[] _rawRow;

        /// <summary>
        /// The previous raw row.
        /// </summary>
        protected byte[] _previousRawRow;

        /// <summary>
        /// The current raw row unfiltered.
        /// </summary>
        protected byte[] _filteredRawRow;

        /// <summary>
        /// Whether or not the PNG was interlaced.
        /// </summary>
        private readonly bool _interlaced;

        /// <summary>
        /// Whether or not to unpack bitdepths of 1, 2, or 4.
        /// </summary>
        private bool _unpackedMode;

        /// <summary>
        /// number of chunk group (0-6) last read, or currently reading
        /// </summary>
        /// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
        private int CurrentChunkGroup { get; set; }

        /// <summary>
        /// Last read row number.
        /// </summary>
        protected int LastRowNum = -1;

        /// <summary>
        /// The offset or number of bytes read in the input stream.
        /// </summary>
        private long _offset = 0;

        /// <summary>
        /// The number of bytes loaded from ancillary chunks.
        /// </summary>
        private int _bytesChunksLoaded = 0;

        /// <summary>
        /// The config for the reader.
        /// </summary>
        private readonly PngReaderConfig _config;

        /// <summary>
        /// Creates a <see cref="PngReader"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        public PngReader(Stream input) : this(input, PngReaderConfig.Default) { }

        /// <summary>
        /// Creates a <see cref="PngReader"/> from a <see cref="Stream"/> and <see cref="PngReaderConfig"/>.
        /// </summary>
        /// <remarks>
        /// Also see <see cref="PngFileHelper.PngOpenRead(string)"/>
        /// </remarks>.
        /// <param name="input">The input stream.</param>
        /// <param name="config">The config for the reader.</param>
        public PngReader(Stream input, PngReaderConfig config)
        {
            _chunksList = new ChunksList();
            _config = config;
            _baseStream = input;
            _offset = 0;

            // Set options
            CurrentChunkGroup = -1;
            _unpackedMode = config.UnpackedMode;

            // Read signature
            byte[] pngid = new byte[8];
            PngHelperInternal.ReadBytes(input, pngid, 0, pngid.Length);
            _offset += pngid.Length;
            if (!ArrayHelper.ArraysEqual(pngid, PngHelperInternal.PNG_ID_SIGNATURE))
                throw new InvalidDataException("Invalid PNG signature.");

            CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;

            // Read first chunk IHDR
            int clen = PngHelperInternal.ReadInt32(input);
            _offset += 4;
            if (clen != 13)
                throw new Exception($"IHDR chunk length was not {13}: {clen}");

            byte[] chunkid = new byte[4];
            PngHelperInternal.ReadBytes(input, chunkid, 0, 4);
            if (!ArrayHelper.ArraysEqual4(chunkid, ChunkHelper.IHDR_BYTES))
                throw new InvalidDataException($"IHDR not found as first chunk: {ChunkHelper.ToString(chunkid)}");

            _offset += 4;
            PngChunkIHDR ihdr = (PngChunkIHDR)ReadChunk(chunkid, clen, false);
            bool alpha = (ihdr.ColorModel & 0x04) != 0;
            bool palette = (ihdr.ColorModel & 0x01) != 0;
            bool grayscale = ihdr.ColorModel == 0 || ihdr.ColorModel == 4;

            // Create ImgInfo and _imageLine, and allocate buffers
            ImgInfo = new ImageInfo(ihdr.Columns, ihdr.Rows, ihdr.BitsPerChannel, alpha, grayscale, palette);
            _rawRow = new byte[ImgInfo.BytesPerRow + 1];
            _previousRawRow = new byte[_rawRow.Length];
            _filteredRawRow = new byte[_rawRow.Length];
            _interlaced = ihdr.Interlaced == 1;
            _deinterlacer = _interlaced ? new PngDeinterlacer(ImgInfo) : null;

            _metadata = new PngMetadata(_chunksList, ImgInfo);

            // Some checks
            if (ihdr.FilterMethod != 0 || ihdr.CompressionMethod != 0 || (ihdr.Interlaced & 0xFFFE) != 0)
                throw new InvalidDataException("CompressionMethod or FilterMethod or Interlaced unrecognized.");
            if (ihdr.ColorModel < 0 || ihdr.ColorModel > 6 || ihdr.ColorModel == 1 || ihdr.ColorModel == 5)
                throw new InvalidDataException($"Invalid ColorModel: {ihdr.ColorModel}");
            if (ihdr.BitsPerChannel != 1 && ihdr.BitsPerChannel != 2 && ihdr.BitsPerChannel != 4 && ihdr.BitsPerChannel != 8 && ihdr.BitsPerChannel != 16)
                throw new InvalidDataException($"Invalid bit depth: {ihdr.BitsPerChannel}");

            var idatLen = ReadFirstChunks();

            _iIdatCstream = new PngIDatChunkInputStream(_baseStream, idatLen, _offset);
            _idatInputstream = ZlibStreamFactory.CreateZlibInputStream(_iIdatCstream, true);
            if (!_config.CheckCrc32)
                _iIdatCstream.DisableCrcCheck();
        }

        #region Methods

        /// <summary>
        /// Returns the ancillary chunks available
        /// </summary>
        /// <remarks>
        /// If the rows have not yet still been read, this includes
        /// only the chunks placed before the pixels (IDAT)
        /// </remarks>
        /// <returns>ChunksList</returns>
        public ChunksList GetChunksList()
        {
            if (FirstChunksNotRead())
                ReadFirstChunks();
            return _chunksList;
        }

        /// <summary>
        /// Returns the ancillary chunks available.
        /// </summary>
        /// <remarks>
        /// See <see cref="GetChunksList"/>.
        /// </remarks>
        /// <returns>A <see cref="PngMetadata"/>.</returns>
        public PngMetadata GetMetadata()
        {
            if (FirstChunksNotRead())
                ReadFirstChunks();

            return _metadata;
        }

        public bool IsInterlaced()
            => _interlaced;

        #endregion

        #region Read Rows

        private void DecodeLastReadRowToInt(int[] buffer, int bytesRead)
        {
            // see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            if (ImgInfo.BitDepth <= 8)
            {
                for (int i = 0, j = 1; i < bytesRead; i++)
                    buffer[i] = _rawRow[j++];
            }
            else
            {
                // 16 bitspc
                for (int i = 0, j = 1; j < bytesRead; i++)
                    buffer[i] = (_rawRow[j++] << 8) + _rawRow[j++];
            }
            if (ImgInfo.Packed && _unpackedMode)
                ImageLine.UnpackInplaceInts(ImgInfo, buffer, buffer, false);
        }

        private void DecodeLastReadRowToByte(byte[] buffer, int bytesRead)
        {
            // see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            if (ImgInfo.BitDepth <= 8)
            {
                Array.Copy(_rawRow, 1, buffer, 0, bytesRead);
            }
            else
            {
                // 16 bitspc
                for (int i = 0, j = 1; j < bytesRead; i++, j += 2)
                    buffer[i] = _rawRow[j]; // 16 bits in 1 byte: this discards the LSB!!!
            }
            if (ImgInfo.Packed && _unpackedMode)
                ImageLine.UnpackInplaceBytes(ImgInfo, buffer, buffer, false);
        }

        /// <summary>
        /// Reads the row using ImageLine as a buffer.
        /// </summary>
        ///<param name="nrow">The number of the row to read for checking.</param>
        /// <returns>The ImageLine that also is available inside this object.</returns>
        public ImageLine ReadRow(int nrow)
            => _imageLine == null || _imageLine.LineSampleType != ImageLine.SampleType.Byte ? ReadRowInt(nrow) : ReadRowByte(nrow);

        public ImageLine ReadRowInt(int nrow)
        {
            _imageLine ??= new ImageLine(ImgInfo, ImageLine.SampleType.Integer, _unpackedMode);

            if (_imageLine.RowNum == nrow) // already read
                return _imageLine;
            ReadRowInt(_imageLine.ScanlineInts, nrow);
            _imageLine.FilterUsed = (FilterType)_filteredRawRow[0];
            _imageLine.RowNum = nrow;
            return _imageLine;
        }

        public ImageLine ReadRowByte(int nrow)
        {
            _imageLine ??= new ImageLine(ImgInfo, ImageLine.SampleType.Byte, _unpackedMode);

            if (_imageLine.RowNum == nrow) // already read
                return _imageLine;

            ReadRowByte(_imageLine.ScanlineBytes, nrow);
            _imageLine.FilterUsed = (FilterType)_filteredRawRow[0];
            _imageLine.RowNum = nrow;
            return _imageLine;
        }

        public int[] ReadRow(int[] buffer, int nrow)
            => ReadRowInt(buffer, nrow);

        public int[] ReadRowInt(int[] buffer, int nrow)
        {
            buffer ??= new int[_unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];

            if (!_interlaced)
            {
                if (nrow <= LastRowNum)
                    throw new InvalidOperationException($"Rows must be read in increasing order: {nrow}");

                // Read rows, perhaps skipping if necessary.
                int bytesread = 0;
                while (LastRowNum < nrow)
                    bytesread = ReadRowRaw(LastRowNum + 1);

                DecodeLastReadRowToInt(buffer, bytesread);
            }
            else if (_deinterlacer != null)
            {
                // Read all image and store it in deinterlacer
                _deinterlacer.ImageInt ??= ReadRowsInt().ScanlinesInts;

                Array.Copy(_deinterlacer.ImageInt[nrow], 0, buffer, 0,
                    _unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked);
            }
            return buffer;
        }

        public byte[] ReadRowByte(byte[] buffer, int nrow)
        {
            buffer ??= new byte[_unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];

            if (_interlaced && _deinterlacer != null)
            {
                // Read all image and store it in deinterlacer.
                _deinterlacer.ImageByte ??= ReadRowsByte().ScanlinesBytes;

                Array.Copy(_deinterlacer.ImageByte[nrow], 0, buffer, 0,
                    _unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked);
            }
            else
            {
                if (nrow <= LastRowNum)
                    throw new InvalidOperationException($"Rows must be read in increasing order: {nrow}");

                // Read rows, perhaps skipping if necessary.
                int bytesread = 0;
                while (LastRowNum < nrow)
                    bytesread = ReadRowRaw(LastRowNum + 1);

                DecodeLastReadRowToByte(buffer, bytesread);
            }
            return buffer;
        }

        public ImageLines ReadRowsInt(int rowOffset, int numRows, int rowStep)
        {
            if (numRows < 0)
                numRows = (ImgInfo.Rows - rowOffset) / rowStep;
            if (rowStep < 1 || rowOffset < 0 || numRows * rowStep + rowOffset > ImgInfo.Rows)
                throw new ArgumentException("Invalid arguments.");

            var imlines = new ImageLines(ImgInfo, ImageLine.SampleType.Integer, _unpackedMode, rowOffset, numRows, rowStep);
            if (_interlaced && _deinterlacer != null)
            {
                int[] buffer = new int[_unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
                for (int p = 1; p <= 7; p++)
                {
                    _deinterlacer.SetPass(p);
                    for (int i = 0; i < _deinterlacer.GetRows(); i++)
                    {
                        int bytesread = ReadRowRaw(i);
                        int j = _deinterlacer.GetCurrentRowReal();
                        int mrow = imlines.ImageRowToMatrixRowStrict(j);
                        if (mrow >= 0)
                        {
                            DecodeLastReadRowToInt(buffer, bytesread);
                            _deinterlacer.DeinterlaceInt(buffer, imlines.ScanlinesInts[mrow], !_unpackedMode);
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < ImgInfo.Rows; j++)
                {
                    int bytesread = ReadRowRaw(j); // Reads and perhaps discards.
                    int mrow = imlines.ImageRowToMatrixRowStrict(j);
                    if (mrow >= 0)
                        DecodeLastReadRowToInt(imlines.ScanlinesInts[mrow], bytesread);
                }
            }
            End();
            return imlines;
        }

        public ImageLines ReadRowsInt()
            => ReadRowsInt(0, ImgInfo.Rows, 1);

        public ImageLines ReadRowsByte(int rowOffset, int nRows, int rowStep)
        {
            if (nRows < 0)
                nRows = (ImgInfo.Rows - rowOffset) / rowStep;
            if (rowStep < 1 || rowOffset < 0 || nRows * rowStep + rowOffset > ImgInfo.Rows)
                throw new ArgumentException("Invalid arguments.");

            var imlines = new ImageLines(ImgInfo, ImageLine.SampleType.Byte, _unpackedMode, rowOffset, nRows, rowStep);
            if (_interlaced && _deinterlacer != null)
            {
                byte[] buf = new byte[_unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
                for (int p = 1; p <= 7; p++)
                {
                    _deinterlacer.SetPass(p);
                    for (int i = 0; i < _deinterlacer.GetRows(); i++)
                    {
                        int bytesread = ReadRowRaw(i);
                        int j = _deinterlacer.GetCurrentRowReal();
                        int mrow = imlines.ImageRowToMatrixRowStrict(j);
                        if (mrow >= 0)
                        {
                            DecodeLastReadRowToByte(buf, bytesread);
                            _deinterlacer.DeinterlaceByte(buf, imlines.ScanlinesBytes[mrow], !_unpackedMode);
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < ImgInfo.Rows; j++)
                {
                    int bytesread = ReadRowRaw(j); // read and perhaps discards
                    int mrow = imlines.ImageRowToMatrixRowStrict(j);
                    if (mrow >= 0)
                        DecodeLastReadRowToByte(imlines.ScanlinesBytes[mrow], bytesread);
                }
            }
            End();
            return imlines;
        }

        public ImageLines ReadRowsByte()
            => ReadRowsByte(0, ImgInfo.Rows, 1);

        private int ReadRowRaw(int rowNum)
        {
            if (rowNum == 0 && FirstChunksNotRead())
                ReadFirstChunks();

            if (rowNum == 0 && _interlaced)
                Array.Clear(_rawRow, 0, _rawRow.Length); // new subimage: reset filters: this is enough, see the swap that happens lines

            // below
            int bytesRead = ImgInfo.BytesPerRow; // NOT including the filter byte
            if (_deinterlacer != null)
            {
                if (rowNum < 0 || rowNum > _deinterlacer.GetRows() || rowNum != 0 && rowNum != _deinterlacer.GetCurrentRowSubImage() + 1)
                    throw new Exception($"Invalid row in interlaced mode: {rowNum}");

                _deinterlacer.SetRow(rowNum);
                bytesRead = (ImgInfo.BitsPerPixel * _deinterlacer.GetColumns() + 7) / 8;
                if (bytesRead < 1)
                    throw new Exception($"Invalid {nameof(bytesRead)}: {bytesRead}");
            }
            else
            {
                // Check for non-interlaced
                if (rowNum < 0 || rowNum >= ImgInfo.Rows || rowNum != LastRowNum + 1)
                    throw new Exception($"Invalid row: {rowNum}");
            }

            LastRowNum = rowNum;

            // swap buffers
            byte[] tmp = _rawRow;
            _rawRow = _previousRawRow;
            _previousRawRow = tmp;

            // loads in rowbfilter "raw" bytes, with filter
            PngHelperInternal.ReadBytes(_idatInputstream, _filteredRawRow, 0, bytesRead + 1);
            _offset = _iIdatCstream.GetOffset();
            if (_offset < 0)
                throw new Exception($"Bad offset: {_offset}");

            if (_config.MaxTotalBytesRead > 0 && _offset >= _config.MaxTotalBytesRead)
                throw new Exception($"Reading IDAT: Maximum total bytes to read exceeded: {_config.MaxTotalBytesRead}, Offset: {_offset}");

            _rawRow[0] = 0;
            UnfilterRow(bytesRead);
            _rawRow[0] = _filteredRawRow[0];
            if (LastRowNum == ImgInfo.Rows - 1 && !_interlaced || _deinterlacer != null && _deinterlacer.AtLastRow())
                ReadLastAndClose();

            return bytesRead;
        }

        public void ReadSkippingAllRows()
        {
            if (FirstChunksNotRead())
                ReadFirstChunks();

            // We read directly from the compressed stream, we don't decompress nor check CRC.
            _iIdatCstream.CheckCrc = false;
            int readCount;
            do
            {
                readCount = _iIdatCstream.Read(_filteredRawRow, 0, _filteredRawRow.Length);
            } while (readCount >= 0);

            _offset = _iIdatCstream.GetOffset();
            if (_offset < 0)
                throw new Exception($"Invalid offset: {_offset}");
            if (_config.MaxTotalBytesRead > 0 && _offset >= _config.MaxTotalBytesRead)
                throw new Exception($"Reading IDAT: Maximum total bytes to read exceeeded: {_config.MaxTotalBytesRead}, Offset: {_offset}");

            ReadLastAndClose();
        }

        #endregion

        #region Read Chunks

        private bool FirstChunksNotRead()
            => CurrentChunkGroup < ChunksList.CHUNK_GROUP_1_AFTERIDHR;

        /// <summary>
        /// Reads chunks before first IDAT.<br/>
        /// Position before: after IDHR (crc included)<br/>
        /// Position after: just after the first IDAT chunk id.<br/>
        /// Returns length of first IDAT chunk, -1 if not found.
        /// </summary>
        private int ReadFirstChunks()
        {
            int clen = 0;
            bool found = false;
            byte[] chunkid = new byte[4]; // It's important to reallocate in each.
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            while (!found)
            {
                clen = PngHelperInternal.ReadInt32(_baseStream);
                _offset += 4;
                if (clen < 0)
                    break;

                PngHelperInternal.ReadBytes(_baseStream, chunkid, 0, 4);
                _offset += 4;

                if (ArrayHelper.ArraysEqual4(chunkid, ChunkHelper.IDAT_BYTES))
                {
                    found = true;
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
                    // add dummy idat chunk to list
                    _chunksList.AppendReadChunk(new PngChunkIDAT(ImgInfo, clen, _offset - 8), CurrentChunkGroup);
                    break;
                }
                else if (ArrayHelper.ArraysEqual4(chunkid, ChunkHelper.IEND_BYTES))
                {
                    throw new InvalidDataException($"END chunk found before image data (IDAT), offset: {_offset}");
                }

                string chunkids = ChunkHelper.ToString(chunkid);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;

                ReadChunk(chunkid, clen, false);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            }

            int idatLen = found ? clen : -1;
            if (idatLen < 0)
                throw new InvalidDataException("First idat chunk not found.");

            return idatLen;
        }

        /// <summary>
        /// Reads (and processes ... up to a point) chunks after last IDAT.
        /// </summary>
        private void ReadLastChunks()
        {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;

            if (!_iIdatCstream.IsEnded())
                _iIdatCstream.ForceChunkEnd();

            int clen = _iIdatCstream.LastChunkLength;
            byte[] chunkid = _iIdatCstream.LastChunkID;
            bool endfound = false;
            bool first = true;

            bool skip;
            while (!endfound)
            {
                skip = false;
                if (!first)
                {
                    clen = PngHelperInternal.ReadInt32(_baseStream);
                    _offset += 4;
                    if (clen < 0)
                        throw new InvalidDataException($"Invalid chunk length: {clen}");

                    PngHelperInternal.ReadBytes(_baseStream, chunkid, 0, 4);
                    _offset += 4;
                }

                first = false;
                if (ArrayHelper.ArraysEqual4(chunkid, ChunkHelper.IDAT_BYTES))
                {
                    // Extra dummy (empty?) idat chunk, it can happen, ignore it.
                    skip = true;
                }
                else if (ArrayHelper.ArraysEqual4(chunkid, ChunkHelper.IEND_BYTES))
                {
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
                    endfound = true;
                }

                ReadChunk(chunkid, clen, skip);
            }

            if (!endfound)
                throw new Exception($"End chunk not found, current offset: {_offset}");
        }

        /// <summary>
        /// Internally called after having read the last line. 
        /// It reads extra chunks after IDAT, if present.
        /// </summary>
        private void ReadLastAndClose()
        {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_5_AFTERIDAT)
            {
                try
                {
                    _idatInputstream.Dispose();
                }
                catch (Exception) { }
                ReadLastChunks();
            }
            Close();
        }

        /// <summary>
        /// Reads chunkd from input stream, adds to ChunksList, and returns it.
        /// If it's skipped, a PngChunkSkipped object is created
        /// </summary>
        /// <returns>The read <see cref="PngChunk"/>.</returns>
        private PngChunk ReadChunk(byte[] chunkid, int clen, bool skipforced)
        {
            if (clen < 0)
                throw new ArgumentException($"Invalid chunk length: {clen}");

            string chunkidstr = ChunkHelper.ToString(chunkid);
            bool critical = ChunkHelper.IsCritical(chunkidstr);
            bool skip = skipforced;
            if (_config.MaxTotalBytesRead > 0 && clen + _offset > _config.MaxTotalBytesRead)
                throw new Exception($"Maximum total bytes to read exceeeded: {_config.MaxTotalBytesRead}, Offset: {_offset}, Chunk length: {clen}");

            // An ancillary chunks can be skipped because of several reasons:
            if (CurrentChunkGroup > ChunksList.CHUNK_GROUP_0_IDHR && !ChunkHelper.IsCritical(chunkidstr))
            {
                // _skipChunksByIdSet is created lazily, if the first IHDR has already been read.
                if (_skipChunkIdsSet == null)
                {
                    _skipChunkIdsSet = [];
                    foreach (string id in _config.SkipChunkIds)
                    {
                        _skipChunkIdsSet.Add(id, 1);
                    }
                }

                skip = skip || _config.SkipChunkMaxSize > 0 && clen >= _config.SkipChunkMaxSize
                    || _skipChunkIdsSet.ContainsKey(chunkidstr)
                    || _config.MaxBytesMetadata > 0 && clen > _config.MaxBytesMetadata - _bytesChunksLoaded
                    || !ChunkHelper.ShouldLoad(chunkidstr, _config.ChunkLoadBehaviour);
            }

            PngChunk pngChunk;
            if (skip)
            {
                PngHelperInternal.SkipBytes(_baseStream, clen);
                PngHelperInternal.ReadInt32(_baseStream); // skip - we dont call PngHelperInternal.skipBytes(inputStream, clen + 4) for risk of overflow 
                pngChunk = new PngChunkSkipped(chunkidstr, ImgInfo, clen);
            }
            else
            {
                var chunk = new ChunkRaw(clen, chunkid, true);
                chunk.ReadChunkData(_baseStream, _config.CheckCrc32 || critical);
                pngChunk = PngChunk.Factory(chunk, ImgInfo);
                if (!pngChunk.Critical)
                {
                    _bytesChunksLoaded += chunk.Length;
                }
            }

            pngChunk.Offset = _offset - 8L;
            _chunksList.AppendReadChunk(pngChunk, CurrentChunkGroup);
            _offset += clen + 4L;
            return pngChunk;
        }

        #endregion

        #region Unfilter

        private void UnfilterRow(int nbytes)
        {
            int ftn = _filteredRawRow[0];
            FilterType ft = (FilterType)ftn;
            switch (ft)
            {
                case FilterType.FILTER_NONE:
                    UnfilterRowNone(nbytes);
                    break;
                case FilterType.FILTER_SUB:
                    UnfilterRowSubtract(nbytes);
                    break;
                case FilterType.FILTER_UP:
                    UnfilterRowUp(nbytes);
                    break;
                case FilterType.FILTER_AVERAGE:
                    UnfilterRowAverage(nbytes);
                    break;
                case FilterType.FILTER_PAETH:
                    UnfilterRowPaeth(nbytes);
                    break;
                default:
                    throw new NotImplementedException($"Filter type {ftn} not implemented.");
            }
        }

        private void UnfilterRowAverage(int nbytes)
        {
            int i;
            int j;
            int x;

            for (j = 1 - ImgInfo.BytesPerPixel, i = 1; i <= nbytes; i++, j++)
            {
                x = j > 0 ? _rawRow[j] : 0;
                _rawRow[i] = (byte)(_filteredRawRow[i] + (x + (_previousRawRow[i] & 0xFF)) / 2);
            }
        }

        private void UnfilterRowNone(int nbytes)
        {
            for (int i = 1; i <= nbytes; i++)
                _rawRow[i] = _filteredRawRow[i];
        }

        private void UnfilterRowPaeth(int nbytes)
        {
            int i;
            int j;
            int x;
            int y;

            for (j = 1 - ImgInfo.BytesPerPixel, i = 1; i <= nbytes; i++, j++)
            {
                x = j > 0 ? _rawRow[j] : 0;
                y = j > 0 ? _previousRawRow[j] : 0;
                _rawRow[i] = (byte)(_filteredRawRow[i] + PngHelperInternal.FilterPaethPredictor(x, _previousRawRow[i], y));
            }
        }

        private void UnfilterRowSubtract(int nbytes)
        {
            int i;
            int j;

            for (i = 1; i <= ImgInfo.BytesPerPixel; i++)
            {
                _rawRow[i] = _filteredRawRow[i];
            }

            for (j = 1, i = ImgInfo.BytesPerPixel + 1; i <= nbytes; i++, j++)
            {
                _rawRow[i] = (byte)(_filteredRawRow[i] + _rawRow[j]);
            }
        }

        private void UnfilterRowUp(int nbytes)
        {
            for (int i = 1; i <= nbytes; i++)
            {
                _rawRow[i] = (byte)(_filteredRawRow[i] + _previousRawRow[i]);
            }
        }

        #endregion

        #region Read Helpers

        /// <summary>
        /// If necessary, creates a chunk cache for commonly re-accessed chunk data.
        /// </summary>
        /// <exception cref="Exception">The transparency chunk was not searched for before palette,</exception>
        private void InitChunkCache()
        {
            if (_plteCacheState == -1 || _trnsCacheState == -1)
            {
                bool trueColor = ImgInfo.Channels >= 3;
                bool grayScale = ImgInfo.Grayscale;
                bool indexed = ImgInfo.Indexed;
                int bitsPerChannel = ImgInfo.BitDepth;
                bool bit8 = bitsPerChannel < 16;
                bool bit16 = bitsPerChannel > 8;
                byte[]? trnsAlpha = null;

                var metadata = GetMetadata();
                if (_trnsCacheState == -1)
                {
                    var trns = metadata.GetTRNS();
                    if (trns != null)
                    {
                        if (indexed)
                            trnsAlpha = trns.GetPaletteAlphaBytes();
                        else if (grayScale && bit8)
                            _trnsGray8 = trns.GetL8();
                        else if (grayScale && bit16)
                            _trnsGray16 = trns.GetL16();
                        else if (trueColor && bit8)
                            _trnsRgb24 = trns.GetRgb24();
                        else if (trueColor && bit16)
                            _trnsRgb48 = trns.GetRgb48();
                        _trnsCacheState = 1;
                    }
                    else
                    {
                        _trnsCacheState = 0;
                    }
                }

                if (_plteCacheState == -1)
                {
                    var plte = metadata.GetPLTE();
                    if (plte != null)
                    {
                        if (_trnsCacheState == 0)
                            _plte = plte.GetColors();
                        else if (_trnsCacheState == 1)
                            if (trnsAlpha == null)
                                throw new Exception("Transparency cache null while state claims it is not.");
                            else
                                _plte = plte.GetColors(trnsAlpha);
                        else if (_trnsCacheState == -1)
                            throw new Exception("Transparency chunk not searched for before palette chunk.");
                        _plteCacheState = 1;
                    }
                    else
                    {
                        _plteCacheState = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Reads the next line as an array of indices.
        /// </summary>
        /// <param name="nrow">The row number, for checking.</param>
        /// <returns>An array of indices.</returns>
        public int[] ReadLineIndices(int nrow)
        {
            if (ImgInfo.BitDepth < 16)
            {
                return ReadRowByte(nrow).GetIndices();
            }

            return ReadRowInt(nrow).GetIndices();
        }

        /// <summary>
        /// Gets palette colors, will be an empty array if they could not be found.
        /// </summary>
        /// <returns>An array of palette colors.</returns>
        public Color[] GetPaletteColors()
        {
            InitChunkCache();
            return _plte ?? [];
        }

        /// <summary>
        /// Reads the next line as an array of colors, making any transformations necessary.
        /// </summary>
        /// <param name="nrow">The row number, for checking.</param>
        /// <returns>An array of colors.</returns>
        /// <exception cref="Exception">Color types were exclusive, the image was indexed but didn't have a palette, or the primary kind of data to read could not be determined.</exception>
        public Color[] ReadLineColors(int nrow)
        {
            bool trueColor = ImgInfo.Channels >= 3;
            bool grayScale = ImgInfo.Grayscale;
            bool indexed = ImgInfo.Indexed;
            bool trueAlpha = ImgInfo.HasAlpha;
            int bitsPerChannel = ImgInfo.BitDepth;
            bool bit8 = bitsPerChannel < 16;
            bool bit16 = bitsPerChannel > 8;
            int width = ImgInfo.Columns;
            InitChunkCache();

            bool hasPlte = _plteCacheState == 1;
            bool hasTrns = _trnsCacheState == 1;

            if (trueColor && (grayScale || indexed)
                || grayScale && (trueColor || indexed)
                || indexed && (grayScale || trueColor))
                throw new Exception("True color, grayscale, and indexed are exclusive, please check reader.");

            if (indexed)
            {
                if (!hasPlte)
                {
                    throw new Exception("Image is indexed but had no palette read.");
                }

                if (_plte == null)
                {
                    throw new Exception("Palette cache was null on indexed image.");
                }

                Color[] colors = new Color[width];
                int[] indices = ReadLineIndices(nrow);
                for (int i = 0; i < indices.Length; i++)
                {
                    colors[i] = _plte[indices[i]];
                }

                return colors;
            }
            else if (grayScale && bit8 && trueAlpha)
            {
                // Grayscale Alpha 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetLa16());
            }
            else if (grayScale && bit16 && trueAlpha)
            {
                // Grayscale Alpha 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetLa32());
            }
            else if (grayScale && bit8 && hasTrns)
            {
                // Grayscale poor Alpha 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetL8(), _trnsGray8);
            }
            else if (grayScale && bit16 && hasTrns)
            {
                // Grayscale poor Alpha 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetL16(), _trnsGray16);
            }
            else if (grayScale && bit8)
            {
                // Grayscale 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetL8());
            }
            else if (grayScale && bit16)
            {
                // Grayscale Alpha 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetL16());
            }
            else if (trueColor && bit8 && trueAlpha)
            {
                // True Color Alpha 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetRgba32());
            }
            else if (trueColor && bit16 && trueAlpha)
            {
                // True Color Alpha 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetRgba64());
            }
            else if (trueColor && bit8 && hasTrns)
            {
                // True Color poor Alpha 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetRgb24(), _trnsRgb24);
            }
            else if (trueColor && bit16 && hasTrns)
            {
                // True Color poor Alpha 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetRgb48(), _trnsRgb48);
            }
            else if (trueColor && bit8)
            {
                // True Color 1-8 bits
                return ColorConvert.ToColor(ReadRowByte(nrow).GetRgb24());
            }
            else if (trueColor && bit16)
            {
                // True Color 16 bits
                return ColorConvert.ToColor(ReadRowInt(nrow).GetRgb48());
            }

            throw new Exception("Could not determine primary image type to read.");
        }

        #endregion

        #region End

        private void Close()
        {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
            {
                // this could only happen if forced close
                try
                {
                    _idatInputstream.Dispose();
                }
                catch (Exception) { }
                CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
            }

            if (!_config.LeaveOpen)
                _baseStream.Dispose();
        }

        /// <summary>
        /// Normally this does nothing, but it can be used to force a premature closing
        /// </summary>
        public void End()
        {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
                Close();
        }

        #endregion
    }
}
