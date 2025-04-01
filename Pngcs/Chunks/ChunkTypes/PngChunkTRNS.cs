using Pngcs.Drawing;
using Pngcs.Helpers;
using System;

namespace Pngcs.Chunks
{
    /// <summary>
    /// tRNS chunk: http://www.w3.org/TR/PNG/#11tRNS
    /// </summary>
    public class PngChunkTRNS : PngChunkSingle
    {
        public const string ID = ChunkHelper.tRNS;

        // this chunk structure depends on the image type
        // only one of these is meaningful
        private int gray;
        private int red, green, blue;
        private int[] paletteAlpha;

        public PngChunkTRNS(ImageInfo info)
            : base(ID, info)
        {
            paletteAlpha = [];
        }

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
                c = CreateEmptyChunk(paletteAlpha.Length, true);
                for (int n = 0; n < c.Length; n++)
                    c.Data[n] = (byte)paletteAlpha[n];
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
                int nentries = c.Data.Length;
                paletteAlpha = new int[nentries];
                for (int n = 0; n < nentries; n++)
                {
                    paletteAlpha[n] = c.Data[n] & 0xff;
                }
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
            PngChunkTRNS otherx = (PngChunkTRNS)other;
            gray = otherx.gray;
            red = otherx.red;
            green = otherx.green;
            blue = otherx.blue;
            if (otherx.paletteAlpha != null)
            {
                paletteAlpha = new int[otherx.paletteAlpha.Length];
                Array.Copy(otherx.paletteAlpha, 0, paletteAlpha, 0, paletteAlpha.Length);
            }
        }

        #region RGB

        public int[] GetRGB()
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            return [red, green, blue];
        }

        public void SetRGB(int r, int g, int b)
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            red = r;
            green = g;
            blue = b;
        }

        public Rgb24 GetRgb24()
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            return new Rgb24((byte)red, (byte)green, (byte)blue);
        }

        public void SetRgb24(Rgb24 color)
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            red = color.Red;
            green = color.Green;
            blue = color.Blue;
        }

        public Rgb48 GetRgb48()
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            return new Rgb48((ushort)red, (ushort)green, (ushort)blue);
        }

        public void SetRgb48(Rgb48 color)
        {
            if (ImgInfo.Grayscale || ImgInfo.Indexed)
                throw new InvalidOperationException("Only RGB or RGBA images support this.");

            red = color.Red;
            green = color.Green;
            blue = color.Blue;
        }

        #endregion

        #region Gray

        public int GetGray()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            return gray;
        }

        public void SetGray(int g)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");


            gray = g;
        }

        public L8 GetL8()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            return new L8((byte)gray);
        }

        public void SetL8(L8 value)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            gray = value.Luminance;
        }

        public L16 GetL16()
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            return new L16((ushort)gray);
        }

        public void SetL16(L16 value)
        {
            if (!ImgInfo.Grayscale)
                throw new InvalidOperationException("Only grayscale images support this.");

            gray = value.Luminance;
        }

        #endregion

        #region Alpha

        /// <summary>
        /// Warning: Not a deep copy.
        /// </summary>
        public int[] GetPaletteAlpha()
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            return paletteAlpha;
        }

        /// <summary>
        /// Warning: Not a deep copy.
        /// </summary>
        public void SetPaletteAlpha(int[] palAlpha)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            paletteAlpha = palAlpha;
        }

        /// <summary>
        /// Get the palette alpha channel as an array of bytes.
        /// </summary>
        /// <returns>An array of bytes representing the alpha channel.</returns>
        /// <exception cref="InvalidOperationException">Only indexed/palette images support this.</exception>
        public byte[] GetPaletteAlphaBytes()
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            byte[] alpha = new byte[paletteAlpha.Length];
            for (int i = 0; i < paletteAlpha.Length; i++)
            {
                alpha[i] = (byte)paletteAlpha[i];
            }
            return alpha;
        }

        /// <summary>
        /// Set the palette alpha channel with an array of bytes.
        /// </summary>
        /// <param name="alpha">An array of bytes representing the alpha channel.</param>
        /// <exception cref="InvalidOperationException">Only indexed/palette images support this.</exception>
        public void SetPaletteAlphaBytes(byte[] alpha)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            for (int i = 0; i < paletteAlpha.Length; i++)
            {
                paletteAlpha[i] = alpha[i];
            }
        }

        public byte GetPaletteAlphaByte(int index)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            return (byte)paletteAlpha[index];
        }

        public void SetPaletteAlphaByte(int index, byte value)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            paletteAlpha[index] = value;
        }

        /// <summary>
        /// Utility method to use when only one pallete index is set as totally transparent.
        /// </summary>
        public void SetIndexEntryAsTransparent(int palAlphaIndex)
        {
            if (!ImgInfo.Indexed)
                throw new InvalidOperationException("Only indexed/palette images support this.");

            paletteAlpha = [palAlphaIndex + 1];
            for (int i = 0; i < palAlphaIndex; i++)
                paletteAlpha[i] = 255;

            paletteAlpha[palAlphaIndex] = 0;
        }

        #endregion
    }
}
