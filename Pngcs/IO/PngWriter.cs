using Pngcs.Chunks;
using Pngcs.Helpers;
using Pngcs.IO.Zlib;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    ///  Writes a PNG image, line by line.
    /// </summary>
    public class PngWriter
    {
        /// <summary>
        /// The output stream.
        /// </summary>
        private readonly Stream _baseStream;

        /// <summary>
        /// The pixel data stream.
        /// </summary>
        private readonly PngIDatChunkOutputStream datStream;

        /// <summary>
        /// The compression stream.
        /// </summary>
        private readonly ZlibOutputStream datStreamDeflated;

        /// <summary>
        /// The filtering strategy.
        /// </summary>
        private FilterWriteStrategy _filterStrategy;

        /// <summary>
        /// Basic image info, immutable.
        /// </summary>
        public readonly ImageInfo _imageInfo;

        /// <summary>
        /// A high level wrapper of a ChunksList : list of written/queued chunks
        /// </summary>
        private readonly PngMetadata _metadata;

        /// <summary>
        /// The written/queued chunks.
        /// </summary>
        private readonly ChunksListForWrite _chunksList;

        /// <summary>
        /// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
        /// </summary>
        protected byte[] _rawRow;

        /// <summary>
        /// The previous raw row.
        /// </summary>
        protected byte[] _previousRawRow;

        /// <summary>
        /// The current raw row, after being filtered.
        /// </summary>
        protected byte[] _filteredRawRow;

        /// <summary>
        /// Number of chunk group (0-6) last written, or currently writing.
        /// </summary>
        /// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
        private int CurrentChunkGroup { get; set; }

        /// <summary>
        /// Current line number.
        /// </summary>
        private int _rowNum = -1;

        /// <summary>
        /// Auxiliar buffer, histogram, only used by <see cref="ReportResultsForFilter"/>.
        /// </summary>
        private readonly int[] histox = new int[256];

        /// <summary>
        /// This only influences the 1-2-4 bitdepth format; If we pass a ImageLine to WriteRow, this is ignored.
        /// </summary>
        private bool _unpackedMode;

        /// <summary>
        /// Whether or not values need to be packed, is auto computed.
        /// </summary>
        private bool needsPack;

        /// <summary>
        /// Whether or not the first chunks have been written.
        /// </summary>
        private bool started;

        /// <summary>
        /// The config for the writer.
        /// </summary>
        private readonly PngWriterConfig _config;

        /// <summary>
        /// Creates a <see cref="PngWriter"/> from a <see cref="Stream"/> and <see cref="ImageInfo"/>.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="imgInfo">The image info.</param>
        public PngWriter(Stream output, ImageInfo imgInfo)
            : this(output, imgInfo, PngWriterConfig.Default) { }

        /// <summary>
        /// Creates a <see cref="PngWriter"/> from a <see cref="Stream"/>, <see cref="ImageInfo"/>, and <see cref="PngWriterConfig"/>.
        /// </summary>
        /// <remarks>
        /// Also see <see cref="PngFileHelper.PngOpenWrite(string, ImageInfo, bool)"/>.
        /// </remarks>
        /// <param name="output">The output stream.</param>
        /// <param name="imageInfo">The image info.</param>
        /// <param name="config">The config for the writer.</param>
        public PngWriter(Stream output, ImageInfo imageInfo, PngWriterConfig config)
        {
            _config = config;
            _baseStream = output;
            _imageInfo = imageInfo;

            _rawRow = new byte[imageInfo.BytesPerRow + 1];
            _previousRawRow = new byte[_rawRow.Length];
            _filteredRawRow = new byte[_rawRow.Length];
            _chunksList = new ChunksListForWrite();
            _metadata = new PngMetadata(_chunksList, _imageInfo);
            _filterStrategy = new FilterWriteStrategy(_imageInfo, config.FilterType);
            _unpackedMode = config.UnpackedMode;
            needsPack = _unpackedMode && imageInfo.Packed;
            started = false;

            // Initialize streams
            datStream = new PngIDatChunkOutputStream(_baseStream, config.IdatMaxSize);
            datStreamDeflated = ZlibStreamFactory.CreateZlibOutputStream(datStream, config.CompressionLevel, config.CompressionStrategy, true);
        }

        #region Methods

        /// <summary>
        /// Gets the <see cref="PngMetadata"/>.
        /// </summary>
        /// <returns>A <see cref="PngMetadata"/>.</returns>
        public PngMetadata GetMetadata()
            => _metadata;

        /// <summary>
        /// Gets the chunks list to be written.
        /// </summary>
        /// <returns>A <see cref="ChunksListForWrite"/>.</returns>
        public ChunksListForWrite GetChunksList()
            => _chunksList;

        /// <summary>
        /// Computes compressed size/raw size, approximate
        /// </summary>
        /// <remarks>Actually: compressed size = total size of IDAT data , raw size = uncompressed pixel bytes = rows * (bytesPerRow + 1)
        /// </remarks>
        /// <returns></returns>
        public double ComputeCompressionRatio()
        {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
                throw new InvalidOperationException($"{nameof(ComputeCompressionRatio)} can only be called after {nameof(End)}");

            return datStream.CountFlushed / (double)((_imageInfo.BytesPerRow + 1) * _imageInfo.Rows);
        }

        #endregion

        #region Start

        public void Start()
        {
            WriteSignatureAndIHDR();
            WriteFirstChunks();
        }

        #endregion

        #region Write Chunks

        private void WriteEndChunk()
            => new PngChunkIEND(_imageInfo).CreateRawChunk().WriteChunk(_baseStream);

        private void WriteFirstChunks()
        {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            _chunksList.WriteChunks(_baseStream, CurrentChunkGroup);

            CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
            int nw = _chunksList.WriteChunks(_baseStream, CurrentChunkGroup);
            if (nw > 0 && _imageInfo.Grayscale)
                throw new InvalidOperationException("Cannot write palette for this format.");
            if (nw == 0 && _imageInfo.Indexed)
                throw new InvalidOperationException("Missing palette.");

            CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            _chunksList.WriteChunks(_baseStream, CurrentChunkGroup);

            CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
        }

        private void WriteLastChunks()
        {
            // Not including end
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            _chunksList.WriteChunks(_baseStream, CurrentChunkGroup);

            // There should be no unwritten chunks.
            List<PngChunk> pending = _chunksList.GetQueuedChunks();
            if (pending.Count > 0)
                throw new Exception($"{pending.Count} chunks were not written.");

            CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
        }

        /// <summary>
        /// Write id signature and also "IHDR" chunk
        /// </summary>
        private void WriteSignatureAndIHDR()
        {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            PngHelperInternal.WriteBytes(_baseStream, PngHelperInternal.PNG_ID_SIGNATURE); // signature

            // http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
            var ihdr = new PngChunkIHDR(_imageInfo)
            {
                Columns = _imageInfo.Columns,
                Rows = _imageInfo.Rows,
                BitsPerChannel = _imageInfo.BitDepth
            };

            int colormodel = 0;
            if (_imageInfo.HasAlpha)
                colormodel += 0x04;
            if (_imageInfo.Indexed)
                colormodel += 0x01;
            if (!_imageInfo.Grayscale)
                colormodel += 0x02;

            ihdr.ColorModel = colormodel;
            ihdr.CompressionMethod = 0; // compression method 0=deflate
            ihdr.FilterMethod = 0; // filter method (0)
            ihdr.Interlaced = 0; // never interlace
            ihdr.CreateRawChunk().WriteChunk(_baseStream);
        }

        #endregion

        #region Write Row

        protected void EncodeRowFromBytes(byte[] row)
        {
            if (row.Length == _imageInfo.SamplesPerRowPacked && !needsPack)
            {
                // some duplication of code - because this case is typical and it works faster this way
                int j = 1;
                if (_imageInfo.BitDepth <= 8)
                {
                    foreach (byte x in row)
                    { // optimized
                        _rawRow[j++] = x;
                    }
                }
                else
                { // 16 bitspc
                    foreach (byte x in row)
                    { // optimized
                        _rawRow[j] = x;
                        j += 2;
                    }
                }
            }
            else
            {
                // perhaps we need to pack?
                if (row.Length >= _imageInfo.SamplesPerRow && needsPack)
                    ImageLine.PackInplaceBytes(_imageInfo, row, row, false); // Row is packed in place!

                if (_imageInfo.BitDepth <= 8)
                {
                    for (int i = 0, j = 1; i < _imageInfo.SamplesPerRowPacked; i++)
                    {
                        _rawRow[j++] = row[i];
                    }
                }
                else
                {
                    // 16 bitspc
                    for (int i = 0, j = 1; i < _imageInfo.SamplesPerRowPacked; i++)
                    {
                        _rawRow[j++] = row[i];
                        _rawRow[j++] = 0;
                    }
                }

            }
        }

        protected void EncodeRowFromInts(int[] row)
        {
            if (row.Length == _imageInfo.SamplesPerRowPacked && !needsPack)
            {
                // Some duplication of code - because this case is typical and it works faster this way.
                int j = 1;
                if (_imageInfo.BitDepth <= 8)
                {
                    foreach (int x in row)
                    {
                        // optimized
                        _rawRow[j++] = (byte)x;
                    }
                }
                else
                {
                    // 16 bitspc
                    foreach (int x in row)
                    {
                        // optimized
                        _rawRow[j++] = (byte)(x >> 8);
                        _rawRow[j++] = (byte)x;
                    }
                }
            }
            else
            {
                // Perhaps we need to pack?
                if (row.Length >= _imageInfo.SamplesPerRow && needsPack)
                    ImageLine.PackInplaceInts(_imageInfo, row, row, false); // Row is packed in place!

                if (_imageInfo.BitDepth <= 8)
                {
                    for (int i = 0, j = 1; i < _imageInfo.SamplesPerRowPacked; i++)
                    {
                        _rawRow[j++] = (byte)row[i];
                    }
                }
                else
                {
                    // 16 bitspc
                    for (int i = 0, j = 1; i < _imageInfo.SamplesPerRowPacked; i++)
                    {
                        _rawRow[j++] = (byte)(row[i] >> 8);
                        _rawRow[j++] = (byte)row[i];
                    }
                }

            }
        }

        private void PrepareEncodeRow(int rown)
        {
            if (!started)
            {
                Start();
                started = true;
            }

            _rowNum++;
            if (rown >= 0 && _rowNum != rown)
                throw new InvalidOperationException($"Rows must be written in order: Expected: {_rowNum}; Passed: {rown}");

            // Swap
            byte[] tmp = _rawRow;
            _rawRow = _previousRawRow;
            _previousRawRow = tmp;
        }

        /// <summary>
        /// Write a <see cref="ImageLine"/>.<br/>
        /// This uses the row number from the <see cref="ImageLine"/>.
        /// </summary>
        public void WriteRow(ImageLine imgline, int rownumber)
        {
            _unpackedMode = imgline.SamplesUnpacked;
            needsPack = _unpackedMode && _imageInfo.Packed;

            if (imgline.LineSampleType == ImageLine.SampleType.Integer)
                WriteRowInt(imgline.ScanlineInts, rownumber);
            else
                WriteRowByte(imgline.ScanlineBytes, rownumber);
        }

        public void WriteRow(int[] newrow)
            => WriteRow(newrow, -1);

        public void WriteRow(int[] newrow, int rowNum)
            => WriteRowInt(newrow, rowNum);

        /// <summary>
        /// Writes a full image row.
        /// </summary>
        /// <remarks>
        /// This must be called sequentially from n=0 to n=rows-1.<br/>
        /// There must be one integer per sample.<br/>
        /// They must be in order: R G B R G B ... (or R G B A R G B A... if it has alpha).<br/>
        /// The values should be between 0 and 255 for 8 bitsperchannel images,<br/>
        /// and between 0-65535 form 16 bitsperchannel images (this applies also to the alpha channel if present)<br/>
        /// The array can be reused.
        /// </remarks>
        /// <param name="newrow">The pixel values to write.</param>
        /// <param name="rowNum">The number of the row, from 0 at the top, to rows - 1 at the bottom.</param>
        public void WriteRowInt(int[] newrow, int rowNum)
        {
            PrepareEncodeRow(rowNum);
            EncodeRowFromInts(newrow);
            FilterAndSend(rowNum);
        }

        /// <summary>
        /// Writes a full image row of bytes.
        /// </summary>
        /// <param name="newrow">The pixel values to write.</param>
        /// <param name="rowNum"></param>
        public void WriteRowByte(byte[] newrow, int rowNum)
        {
            PrepareEncodeRow(rowNum);
            EncodeRowFromBytes(newrow);
            FilterAndSend(rowNum);
        }

        /// <summary>
        /// Writes all the pixels, calling <see cref="WriteRowInt"/> for each image row
        /// </summary>
        /// <param name="image">The image to write.</param>
        public void WriteRowsInt(int[][] image)
        {
            for (int i = 0; i < _imageInfo.Rows; i++)
                WriteRowInt(image[i], i);
        }

        /// <summary>
        /// Writes all the pixels, calling <see cref="WriteRowByte"/> for each image row.
        /// </summary>
        /// <param name="image">The image to write.</param>
        public void WriteRowsByte(byte[][] image)
        {
            for (int i = 0; i < _imageInfo.Rows; i++)
                WriteRowByte(image[i], i);
        }

        #endregion

        #region Filter

        private void FilterAndSend(int rown)
        {
            FilterRow(rown);
            datStreamDeflated.Write(_filteredRawRow, 0, _imageInfo.BytesPerRow + 1);
        }

        private void FilterRow(int rown)
        {
            // Warning: filters operation rely on: "previous row" (rowbprev) is initialized to 0 the first time
            if (_filterStrategy.ShouldTestAll(rown))
            {
                FilterRowNone();
                ReportResultsForFilter(rown, FilterType.FILTER_NONE, true);
                FilterRowSubtract();
                ReportResultsForFilter(rown, FilterType.FILTER_SUB, true);
                FilterRowUp();
                ReportResultsForFilter(rown, FilterType.FILTER_UP, true);
                FilterRowAverage();
                ReportResultsForFilter(rown, FilterType.FILTER_AVERAGE, true);
                FilterRowPaeth();
                ReportResultsForFilter(rown, FilterType.FILTER_PAETH, true);
            }

            FilterType filterType = _filterStrategy.GimmeFilterType(rown, true);
            _filteredRawRow[0] = (byte)(int)filterType;
            switch (filterType)
            {
                case FilterType.FILTER_NONE:
                    FilterRowNone();
                    break;
                case FilterType.FILTER_SUB:
                    FilterRowSubtract();
                    break;
                case FilterType.FILTER_UP:
                    FilterRowUp();
                    break;
                case FilterType.FILTER_AVERAGE:
                    FilterRowAverage();
                    break;
                case FilterType.FILTER_PAETH:
                    FilterRowPaeth();
                    break;
                default:
                    throw new NotImplementedException($"Filter type {filterType} not implemented.");
            }

            ReportResultsForFilter(rown, filterType, false);
        }

        private void FilterRowAverage()
        {
            int i, j, imax;
            imax = _imageInfo.BytesPerRow;
            for (j = 1 - _imageInfo.BytesPerPixel, i = 1; i <= imax; i++, j++)
                _filteredRawRow[i] = (byte)(_rawRow[i] - (_previousRawRow[i] + (j > 0 ? _rawRow[j] : 0)) / 2);
        }

        private void FilterRowNone()
        {
            for (int i = 1; i <= _imageInfo.BytesPerRow; i++)
                _filteredRawRow[i] = _rawRow[i];
        }


        private void FilterRowPaeth()
        {
            int i, j, imax;
            imax = _imageInfo.BytesPerRow;
            for (j = 1 - _imageInfo.BytesPerPixel, i = 1; i <= imax; i++, j++)
                _filteredRawRow[i] = (byte)(_rawRow[i] - PngHelperInternal.FilterPaethPredictor(
                    j > 0 ? _rawRow[j] : 0, _previousRawRow[i], j > 0 ? _previousRawRow[j] : 0));
        }

        private void FilterRowSubtract()
        {
            int i;
            int j;

            for (i = 1; i <= _imageInfo.BytesPerPixel; i++)
                _filteredRawRow[i] = _rawRow[i];

            for (j = 1, i = _imageInfo.BytesPerPixel + 1; i <= _imageInfo.BytesPerRow; i++, j++)
                _filteredRawRow[i] = (byte)(_rawRow[i] - _rawRow[j]);
        }

        private void FilterRowUp()
        {
            for (int i = 1; i <= _imageInfo.BytesPerRow; i++)
                _filteredRawRow[i] = (byte)(_rawRow[i] - _previousRawRow[i]);
        }

        private long SumFilteredRawRow()
        {
            // Sums absolute value.
            long sum = 0;
            for (int i = 1; i <= _imageInfo.BytesPerRow; i++)
            {
                if (_filteredRawRow[i] < 0)
                {
                    sum -= _filteredRawRow[i];
                }
                else
                {
                    sum += _filteredRawRow[i];
                }
            }
            return sum;
        }

        private void ReportResultsForFilter(int rown, FilterType type, bool tentative)
        {
            for (int i = 0; i < histox.Length; i++)
                histox[i] = 0;
            int s = 0, v;
            for (int i = 1; i <= _imageInfo.BytesPerRow; i++)
            {
                v = _filteredRawRow[i];
                if (v < 0)
                    s -= v;
                else
                    s += v;
                histox[v & 0xFF]++;
            }
            _filterStrategy.FillResultsForFilter(rown, type, s, histox, tentative);
        }

        #endregion

        #region End

        /// <summary>
        /// Finalizes the image creation and closes the file stream.
        /// </summary>
        /// <remarks>This must be called after writing all lines.</remarks>
        public void End()
        {
            if (_rowNum != _imageInfo.Rows - 1)
                throw new InvalidOperationException("Not all rows have been written.");

            datStreamDeflated.Dispose();
            datStream.Dispose();
            WriteLastChunks();
            WriteEndChunk();

            if (!_config.LeaveOpen)
            {
                _baseStream.Dispose();
            }
        }

        #endregion
    }
}
