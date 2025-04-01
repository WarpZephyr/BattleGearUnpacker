using System;

namespace Pngcs
{
    /// <summary>
    /// Wraps a set of rows from a image, read in a single operation, stored in a int[][] or byte[][] matrix.<br/>
    /// They can be a subset of the total rows, but in this case they are equispaced.<br/>
    /// Also see <see cref="ImageLine"/>.
    /// </summary>
    public class ImageLines
    {
        public ImageInfo ImgInfo { get; private set; }
        public ImageLine.SampleType SampleType { get; private set; }
        public bool SamplesUnpacked { get; private set; }
        public int RowOffset { get; private set; }

        /// <summary>
        /// The number of rows.
        /// </summary>
        public int RowCount { get; private set; }
        public int RowStep { get; private set; }
        internal readonly int channels;
        internal readonly int bitDepth;
        internal readonly int elementsPerRow;
        public int[][] ScanlinesInts { get; private set; }
        public byte[][] ScanlinesBytes { get; private set; }

        public ImageLines(ImageInfo imageInfo, ImageLine.SampleType sampleType, bool unpackedMode, int rowOffset, int nRows, int rowStep)
        {
            ImgInfo = imageInfo;
            channels = imageInfo.Channels;
            bitDepth = imageInfo.BitDepth;
            SampleType = sampleType;
            SamplesUnpacked = unpackedMode || !imageInfo.Packed;
            RowOffset = rowOffset;
            RowCount = nRows;
            RowStep = rowStep;
            elementsPerRow = unpackedMode ? imageInfo.SamplesPerRow : imageInfo.SamplesPerRowPacked;
            if (sampleType == ImageLine.SampleType.Integer)
            {
                ScanlinesInts = new int[nRows][];
                for (int i = 0; i < nRows; i++) ScanlinesInts[i] = new int[elementsPerRow];
                ScanlinesBytes = [];
            }
            else if (sampleType == ImageLine.SampleType.Byte)
            {
                ScanlinesBytes = new byte[nRows][];
                for (int i = 0; i < nRows; i++) ScanlinesBytes[i] = new byte[elementsPerRow];
                ScanlinesInts = [];
            }
            else
            {
                throw new InvalidOperationException($"Unknown {nameof(ImageLine.SampleType)}: {sampleType}");
            }
        }

        /// <summary>
        /// Translates from image row number to matrix row.
        /// If you are not sure if this image row in included, use better ImageRowToMatrixRowStrict
        /// 
        /// </summary>
        /// <param name="imrow">Row number in the original image (from 0) </param>
        /// <returns>Row number in the wrapped matrix. Undefined result if invalid</returns>
        public int ImageRowToMatrixRow(int imrow)
        {
            int r = (imrow - RowOffset) / RowStep;
            return r < 0 ? 0 : (r < RowCount ? r : RowCount - 1);
        }

        /// <summary>
        /// translates from image row number to matrix row
        /// </summary>
        /// <param name="imrow">Row number in the original image (from 0) </param>
        /// <returns>Row number in the wrapped matrix. Returns -1 if invalid</returns>
        public int ImageRowToMatrixRowStrict(int imrow)
        {
            imrow -= RowOffset;
            int mrow = imrow >= 0 && imrow % RowStep == 0 ? imrow / RowStep : -1;
            return mrow < RowCount ? mrow : -1;
        }

        /// <summary>
        /// Translates from matrix row number to real image row number
        /// </summary>
        /// <param name="mrow"></param>
        /// <returns></returns>
        public int MatrixRowToImageRow(int mrow)
            => mrow * RowStep + RowOffset;

        /// <summary>
        /// Constructs and returns an ImageLine object backed by a matrix row.
        /// This is quite efficient, no deep copy.
        /// </summary>
        /// <param name="mrow">Row number inside the matrix</param>
        /// <returns></returns>
        public ImageLine GetImageLineAtMatrixRow(int mrow)
        {
            if (mrow < 0 || mrow > RowCount)
                throw new ArgumentException($"Bad row {mrow}. Should be positive and less than {RowCount}", nameof(mrow));
            ImageLine imline = SampleType == ImageLine.SampleType.Integer ? new ImageLine(ImgInfo, SampleType,
                    SamplesUnpacked, ScanlinesInts[mrow], null) : new ImageLine(ImgInfo, SampleType,
                    SamplesUnpacked, null, ScanlinesBytes[mrow]);
            imline.RowNum = MatrixRowToImageRow(mrow);
            return imline;
        }
    }
}
