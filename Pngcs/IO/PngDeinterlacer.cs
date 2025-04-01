using System;

namespace Pngcs.IO
{
    /// <summary>
    /// Deinterlaces read PNG data.
    /// </summary>
    internal class PngDeinterlacer
    {
        private readonly ImageInfo _imageInfo;

        /// <summary>
        /// Current pass number (1-7).
        /// </summary>
        private int _pass;

        // Values at the current pass
        private int _rows;
        private int _columns;
        private int _dY;
        private int _dX;
        private int _oY;
        private int _oX;
        private int _oXsamples;
        private int _dXsamples;

        // Current row in the virtual subsampled image; this increments from 0 to cols/dy 7 times.
        private int _currentRowSubImage = -1;

        // In the real image, this will cycle from 0 to im.rows in different steps, 7 times
        private int _currentRowReal = -1;
        private readonly int _packedValsPerPixel;
        private readonly int _packedMask;
        private readonly int _packedShift;

        public int[][]? ImageInt { get; set; } // FULL image -only used for PngReader as temporary storage
        public byte[][]? ImageByte { get; set; }

        internal PngDeinterlacer(ImageInfo imageInfo)
        {
            _imageInfo = imageInfo;
            _pass = 0;
            if (_imageInfo.Packed)
            {
                _packedValsPerPixel = 8 / _imageInfo.BitDepth;
                _packedShift = _imageInfo.BitDepth;
                if (_imageInfo.BitDepth == 1)
                    _packedMask = 0x80;
                else if (_imageInfo.BitDepth == 2)
                    _packedMask = 0xc0;
                else
                    _packedMask = 0xf0;
            }
            else
            {
                _packedMask = _packedShift = _packedValsPerPixel = 1; // Don't care.
            }

            SetPass(1);
            SetRow(0);
        }


        // This refers to the row currentRowSubImage.
        internal void SetRow(int n)
        {
            _currentRowSubImage = n;
            _currentRowReal = n * _dY + _oY;
            if (_currentRowReal < 0 || _currentRowReal >= _imageInfo.Rows)
                throw new Exception("Bad row, this should not happen.");
        }

        internal void SetPass(int pass)
        {
            if (_pass == pass)
                return;

            _pass = pass;
            switch (_pass)
            {
                case 1:
                    _dY = _dX = 8;
                    _oX = _oY = 0;
                    break;
                case 2:
                    _dY = _dX = 8;
                    _oX = 4;
                    _oY = 0;
                    break;
                case 3:
                    _dX = 4;
                    _dY = 8;
                    _oX = 0;
                    _oY = 4;
                    break;
                case 4:
                    _dX = _dY = 4;
                    _oX = 2;
                    _oY = 0;
                    break;
                case 5:
                    _dX = 2;
                    _dY = 4;
                    _oX = 0;
                    _oY = 2;
                    break;
                case 6:
                    _dX = _dY = 2;
                    _oX = 1;
                    _oY = 0;
                    break;
                case 7:
                    _dX = 1;
                    _dY = 2;
                    _oX = 0;
                    _oY = 1;
                    break;
                default:
                    throw new Exception($"Bad interlace pass: {_pass}");
            }

            _rows = (_imageInfo.Rows - _oY) / _dY + 1;
            if ((_rows - 1) * _dY + _oY >= _imageInfo.Rows)
                _rows--; // can be 0

            _columns = (_imageInfo.Columns - _oX) / _dX + 1;
            if ((_columns - 1) * _dX + _oX >= _imageInfo.Columns)
                _columns--; // can be 0

            if (_columns == 0)
                _rows = 0; // really...

            _dXsamples = _dX * _imageInfo.Channels;
            _oXsamples = _oX * _imageInfo.Channels;
        }

        // notice that this is a "partial" deinterlace, it will be called several times for the same row!
        internal void DeinterlaceInt(int[] src, int[] dst, bool readInPackedFormat)
        {
            if (!(_imageInfo.Packed && readInPackedFormat))
                for (int i = 0, j = _oXsamples; i < _columns * _imageInfo.Channels; i += _imageInfo.Channels, j += _dXsamples)
                    for (int k = 0; k < _imageInfo.Channels; k++)
                        dst[j + k] = src[i + k];
            else
                DeinterlaceIntPacked(src, dst);
        }

        // interlaced+packed = monster; this is very clumsy!
        private void DeinterlaceIntPacked(int[] src, int[] dst)
        {
            // Source byte position, bits to shift to left (0,1,2,3,4)
            int spos;
            int smod;
            int smask;

            int tpos;
            int tmod;
            int p;
            int d;

            // Can this really work?
            smask = _packedMask;
            smod = -1;
            for (int i = 0, j = _oX; i < _columns; i++, j += _dX)
            {
                spos = i / _packedValsPerPixel;
                smod += 1;
                if (smod >= _packedValsPerPixel)
                    smod = 0;

                smask >>= _packedShift; // The source mask cycles.
                if (smod == 0)
                    smask = _packedMask;

                tpos = j / _packedValsPerPixel;
                tmod = j % _packedValsPerPixel;
                p = src[spos] & smask;
                d = tmod - smod;
                if (d > 0)
                    p >>= d * _packedShift;
                else if (d < 0)
                    p <<= -d * _packedShift;

                dst[tpos] |= p;
            }
        }

        // Yes, duplication of code is evil, normally.
        internal void DeinterlaceByte(byte[] src, byte[] dst, bool readInPackedFormat)
        {
            if (!(_imageInfo.Packed && readInPackedFormat))
            {
                for (int i = 0, j = _oXsamples; i < _columns * _imageInfo.Channels; i += _imageInfo.Channels, j += _dXsamples)
                {
                    for (int k = 0; k < _imageInfo.Channels; k++)
                    {
                        dst[j + k] = src[i + k];
                    }
                }
            }
            else
            {
                DeinterlacePackedByte(src, dst);
            }
        }

        private void DeinterlacePackedByte(byte[] src, byte[] dst)
        {
            // Source byte position, bits to shift to left (0,1,2,3,4)
            int spos;
            int smod;
            int smask;

            int tpos;
            int tmod;
            int p;
            int d;

            // What the heck are you reading here? I told you would not enjoy this. Try Dostoyevsky or Simone Weil instead
            smask = _packedMask;
            smod = -1;

            // Arrays.fill(dst, 0);
            for (int i = 0, j = _oX; i < _columns; i++, j += _dX)
            {
                spos = i / _packedValsPerPixel;
                smod += 1;
                if (smod >= _packedValsPerPixel)
                    smod = 0;

                smask >>= _packedShift; // The source mask cycles
                if (smod == 0)
                    smask = _packedMask;

                tpos = j / _packedValsPerPixel;
                tmod = j % _packedValsPerPixel;
                p = src[spos] & smask;
                d = tmod - smod;

                if (d > 0)
                    p >>= d * _packedShift;
                else if (d < 0)
                    p <<= -d * _packedShift;

                dst[tpos] |= (byte)p;
            }
        }

        /// <summary>
        /// Whether or not the current row is the last row for the last pass.
        /// </summary>
        internal bool AtLastRow()
            => _pass == 7 && _currentRowSubImage == _rows - 1;

        /// <summary>
        /// Current row number inside the "sub image"
        /// </summary>
        internal int GetCurrentRowSubImage()
            => _currentRowSubImage;

        /// <summary>
        /// Current row number inside the "real image".
        /// </summary>
        internal int GetCurrentRowReal()
            => _currentRowReal;

        /// <summary>
        /// How many rows has the current pass?
        /// </summary>
        internal int GetRows()
            => _rows;

        /// <summary>
        /// Get the number of columns (pixels) there are in the current row.
        /// </summary>
        internal int GetColumns()
            => _columns;
    }
}
