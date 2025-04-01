using Pngcs.Drawing;
using System;
using System.Drawing;

namespace Pngcs.Chunks
{
    /// <summary>
    /// PLTE Palette chunk: this is the only optional critical chunk.<br/>
    /// http://www.w3.org/TR/PNG/#11PLTE
    /// </summary>
    public class PngChunkPLTE : PngChunkSingle
    {
        public const string ID = ChunkHelper.PLTE;

        /// <summary>
        /// The length of the entries.
        /// </summary>
        private int _length = 0;

        /// <summary>
        /// The palette entries.
        /// </summary>
        private int[] entries;

        public PngChunkPLTE(ImageInfo info) : base(ID, info)
        {
            _length = 2 << (info.BitDepth - 1);
            entries = new int[_length];
        }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NA;

        public override ChunkRaw CreateRawChunk()
        {
            int length = 3 * _length;
            int[] rgb = new int[3];
            ChunkRaw rawChunk = CreateEmptyChunk(length, true);
            for (int n = 0, i = 0; n < _length; n++)
            {
                GetEntryRgb(n, rgb);
                rawChunk.Data[i++] = (byte)rgb[0];
                rawChunk.Data[i++] = (byte)rgb[1];
                rawChunk.Data[i++] = (byte)rgb[2];
            }
            return rawChunk;
        }

        public override void ParseFromRaw(ChunkRaw chunk)
        {
            SetLength(chunk.Length / 3);
            for (int n = 0, i = 0; n < _length; n++)
            {
                SetEntry(n, chunk.Data[i++] & 0xff, chunk.Data[i++] & 0xff,
                        chunk.Data[i++] & 0xff);
            }
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkPLTE otherx = (PngChunkPLTE)other;
            SetLength(otherx.GetLength());
            Array.Copy(otherx.entries, 0, entries, 0, _length);
        }

        /// <summary>
        /// Also allocates array.
        /// </summary>
        /// <param name="length">1-256</param>
        public void SetLength(int length)
        {
            if (length < 1 || length > 256)
                throw new ArgumentException($"Invalid palette length: {length}");

            _length = length;
            if (entries == null || entries.Length != length)
                entries = new int[length];
        }

        public int GetLength()
            => _length;

        public void SetEntry(int index, int r, int g, int b)
            => entries[index] = (r << 16) | (g << 8) | b;

        /// <summary>
        /// Get an entry as a packed RGB8 value.
        /// </summary>
        /// <param name="index">The index of the entry.</param>
        /// <returns></returns>
        public int GetEntry(int index)
            => entries[index];


        /// <summary>
        /// Gets the entry at the specified index and splits it among the specified array at the specified offset.
        /// </summary>
        /// <param name="index">The index of the entry to get.</param>
        /// <param name="rgb">The array to fill.</param>
        /// <param name="offset">The offset to start filling from in the array.</param>
        public void GetEntryRgb(int index, int[] rgb, int offset)
        {
            int v = entries[index];
            rgb[offset] = (v & 0xff0000) >> 16;
            rgb[offset + 1] = (v & 0xff00) >> 8;
            rgb[offset + 2] = v & 0xff;
        }

        /// <summary>
        /// shortcut: GetEntryRgb(index, int[] rgb, 0)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="rgb"></param>
        public void GetEntryRgb(int n, int[] rgb)
            => GetEntryRgb(n, rgb, 0);

        /// <summary>
        /// Gets all the entries as an array of <see cref="Color"/>.
        /// </summary>
        /// <returns>An array of <see cref="Color"/>.</returns>
        public Color[] GetColors()
        {
            Color[] colors = new Color[_length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                colors[i] = Color.FromArgb(255, (entry >> 16) & 0xFF, (entry >> 8) & 0xFF, entry & 0xFF);
            }
            return colors;
        }

        /// <summary>
        /// Gets all the entries as an array of <see cref="Color"/>.
        /// </summary>
        /// <param name="alpha">The alpha channel from a transparency chunk.</param>
        /// <returns>An array of <see cref="Color"/>.</returns>
        public Color[] GetColors(byte[] alpha)
        {
            // There can be less alpha values than palette colors
            if (alpha.Length < _length)
            {
                byte[] newAlpha = new byte[_length];
                Array.Copy(alpha, newAlpha, alpha.Length);
                for (int i = alpha.Length; i < newAlpha.Length; i++)
                {
                    // Assumed to be 255.
                    newAlpha[i] = 255;
                }

                alpha = newAlpha;
            }

            Color[] colors = new Color[_length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                colors[i] = Color.FromArgb(alpha[i], (entry >> 16) & 0xFF, (entry >> 8) & 0xFF, entry & 0xFF);
            }
            return colors;
        }

        /// <summary>
        /// Sets all the entries in the palette, allocating a new array.
        /// </summary>
        /// <param name="colors">The colors to set.</param>
        /// <exception cref="ArgumentException">The palette length was invalid.</exception>
        public void SetColors(Color[] colors)
        {
            int length = colors.Length;
            if (length < 1 || length > 256)
                throw new ArgumentException($"Invalid palette length: {length}");

            _length = length;
            entries = new int[length];
            for (int i = 0; i < length; i++)
            {
                var color = colors[i];
                entries[i] = (color.R << 16) | (color.G << 8) | (color.B);
            }
        }

        /// <summary>
        /// Gets all the entries as an array of <see cref="Rgb24"/>.
        /// </summary>
        /// <returns>An array of <see cref="Rgb24"/>.</returns>
        public Rgb24[] GetRgb24()
        {
            Rgb24[] colors = new Rgb24[_length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                colors[i] = new Rgb24((byte)((entry >> 16) & 0xFF), (byte)((entry >> 8) & 0xFF), (byte)(entry & 0xFF));
            }
            return colors;
        }

        /// <summary>
        /// Sets all the entries in the palette, allocating a new array.
        /// </summary>
        /// <param name="colors">The colors to set.</param>
        /// <exception cref="ArgumentException">The palette length was invalid.</exception>
        public void SetColors(Rgb24[] colors)
        {
            int length = colors.Length;
            if (length < 1 || length > 256)
                throw new ArgumentException($"Invalid palette length: {length}");

            _length = length;
            entries = new int[length];
            for (int i = 0; i < length; i++)
            {
                var color = colors[i];
                entries[i] = (color.Red << 16) | (color.Green << 8) | (color.Blue);
            }
        }

        /// <summary>
        /// Gets all the entries as an array of <see cref="Rgba32"/>.
        /// </summary>
        /// <param name="alpha">The alpha channel from a transparency chunk.</param>
        /// <returns>An array of <see cref="Rgba32"/>.</returns>
        public Rgba32[] GetRgba32(byte[] alpha)
        {
            // There can be less alpha values than palette colors
            if (alpha.Length < _length)
            {
                byte[] newAlpha = new byte[_length];
                Array.Copy(alpha, newAlpha, alpha.Length);
                for (int i = alpha.Length; i < newAlpha.Length; i++)
                {
                    // Assumed to be 255.
                    newAlpha[i] = 255;
                }

                alpha = newAlpha;
            }

            Rgba32[] colors = new Rgba32[_length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                colors[i] = new Rgba32((byte)((entry >> 16) & 0xFF), (byte)((entry >> 8) & 0xFF), (byte)(entry & 0xFF), alpha[i]);
            }
            return colors;
        }

        /// <summary>
        /// Sets all the entries in the palette, allocating a new array.
        /// </summary>
        /// <param name="colors">The colors to set.</param>
        /// <exception cref="ArgumentException">The palette length was invalid.</exception>
        public void SetColors(Rgba32[] colors)
        {
            int length = colors.Length;
            if (length < 1 || length > 256)
                throw new ArgumentException($"Invalid palette length: {length}");

            _length = length;
            entries = new int[length];
            for (int i = 0; i < length; i++)
            {
                var color = colors[i];
                entries[i] = (color.Red << 16) | (color.Green << 8) | (color.Blue);
            }
        }

        public Color GetColor(int index)
            => Color.FromArgb(byte.MaxValue, (entries[index] >> 16) & 0xFF, (entries[index] >> 8) & 0xFF, entries[index] & 0xFF);

        public Color GetColor(int index, byte alpha)
            => Color.FromArgb(alpha, (entries[index] >> 16) & 0xFF, (entries[index] >> 8) & 0xFF, entries[index] & 0xFF);

        public void SetColor(int index, Color color)
            => entries[index] = (color.R << 16) | (color.G << 8) | (color.B);

        public Rgb24 GetRgb24(int index)
            => new(unchecked((byte)((entries[index] >> 16) & 0xFF)),
                unchecked((byte)((entries[index] >> 8) & 0xFF)),
                unchecked((byte)(entries[index] & 0xFF)));

        public void SetRgb24(int index, Rgb24 color)
            => entries[index] = (color.Red << 16) | (color.Green << 8) | (color.Blue);

        public Rgba32 GetRgba32(int index)
            => new(unchecked((byte)((entries[index] >> 16) & 0xFF)),
                unchecked((byte)((entries[index] >> 8) & 0xFF)),
                unchecked((byte)(entries[index] & 0xFF)), byte.MaxValue);

        public Rgba32 GetRgba32(int index, byte alpha)
            => new(unchecked((byte)((entries[index] >> 16) & 0xFF)),
                unchecked((byte)((entries[index] >> 8) & 0xFF)),
                unchecked((byte)(entries[index] & 0xFF)), alpha);

        public void SetRgba32(int index, Rgba32 color)
            => entries[index] = (color.Red << 16) | (color.Green << 8) | (color.Blue);

        /// <summary>
        /// Get the minimum allowed bit depth, given the palette size.
        /// </summary>
        /// <returns>1-2-4-8</returns>
        public int MinBitDepth()
        {
            if (_length <= 2)
                return 1;
            else if (_length <= 4)
                return 2;
            else if (_length <= 16)
                return 4;

            return 8;
        }
    }
}