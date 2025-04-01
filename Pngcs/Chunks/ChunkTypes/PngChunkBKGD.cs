using Pngcs.Helpers;
using System;

namespace Pngcs.Chunks
{
    /// <summary>
    /// bKGD chunk, see http://www.w3.org/TR/PNG/#11bKGD
    /// </summary>
    public class PngChunkBKGD : PngChunkSingle
    {
        public const string ID = ChunkHelper.bKGD;

        // this chunk structure depends on the image type
        // only one of these is meaningful
        private int gray;
        private int red, green, blue;
        private int paletteIndex;

        public PngChunkBKGD(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c;
            if (ImgInfo.Grayscale)
            {
                c = CreateEmptyChunk(2, true);
                PngHelperInternal.WriteInt16(gray, c.Data, 0);
            }
            else if (ImgInfo.Indexed)
            {
                c = CreateEmptyChunk(1, true);
                c.Data[0] = (byte)paletteIndex;
            }
            else
            {
                c = CreateEmptyChunk(6, true);
                PngHelperInternal.WriteInt16(red, c.Data, 0);
                PngHelperInternal.WriteInt16(green, c.Data, 0);
                PngHelperInternal.WriteInt16(blue, c.Data, 0);
            }
            return c;
        }

        public override void ParseFromRaw(ChunkRaw c)
        {
            if (ImgInfo.Grayscale)
            {
                gray = PngHelperInternal.ReadInt16(c.Data, 0);
            }
            else if (ImgInfo.Indexed)
            {
                paletteIndex = c.Data[0] & 0xff;
            }
            else
            {
                red = PngHelperInternal.ReadInt16(c.Data, 0);
                green = PngHelperInternal.ReadInt16(c.Data, 2);
                blue = PngHelperInternal.ReadInt16(c.Data, 4);
            }
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkBKGD otherx = (PngChunkBKGD)other;
            gray = otherx.gray;
            red = otherx.red;
            green = otherx.red;
            blue = otherx.red;
            paletteIndex = otherx.paletteIndex;
        }

        /// <summary>
        /// Set gray value (0-255 if bitdept=8)
        /// </summary>
        /// <param name="gray"></param>
        public void SetGray(int gray)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            this.gray = gray;
        }

        /// <summary>
        /// Gets gray value 
        /// </summary>
        /// <returns>gray value  (0-255 if bitdept=8)</returns>
        public int GetGray()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            return gray;
        }

        /// <summary>
        /// Set pallette index - only for indexed
        /// </summary>
        /// <param name="index"></param>
        public void SetPaletteIndex(int index)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            paletteIndex = index;
        }

        /// <summary>
        /// Get pallette index - only for indexed
        /// </summary>
        /// <returns></returns>
        public int GetPaletteIndex()
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            return paletteIndex;
        }

        /// <summary>
        /// Sets rgb value, only for rgb images
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public void SetRGB(int r, int g, int b)
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            red = r;
            green = g;
            blue = b;
        }

        /// <summary>
        /// Gets rgb value, only for rgb images
        /// </summary>
        /// <returns>[r , g, b] array</returns>
        public int[] GetRGB()
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            return new int[] { red, green, blue };
        }
    }
}
