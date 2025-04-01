using System.Drawing;
using System.IO;

namespace Pngcs.Drawing
{
    /// <summary>
    /// A mass color converter.
    /// </summary>
    internal static class ColorConvert
    {
        #region Merge Alpha

        public static Rgba32[] MergeAlpha(Rgb24[] colors, byte[] alpha)
        {
            Rgba32[] newColors = new Rgba32[colors.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = colors[i].ToRgba32(alpha[i]);
            }

            // If alpha was shorter than colors
            for (int i = alpha.Length; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToRgba32();
            }

            return newColors;
        }

        public static Rgba64[] MergeAlpha(Rgb48[] colors, ushort[] alpha)
        {
            Rgba64[] newColors = new Rgba64[colors.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = colors[i].ToRgba64(alpha[i]);
            }

            // If alpha was shorter than colors
            for (int i = alpha.Length; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToRgba64();
            }

            return newColors;
        }

        public static Color[] MergeAlpha(Color[] colors, byte[] alpha)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = Color.FromArgb(alpha[i], colors[i]);
            }

            // If alpha was shorter than colors
            for (int i = alpha.Length; i < colors.Length; i++)
            {
                newColors[i] = Color.FromArgb(255, colors[i]);
            }

            return newColors;
        }

        #endregion

        #region Split Alpha

        public static void SplitAlpha(Rgba32[] colors, out Rgb24[] newColors, out byte[] alpha)
        {
            newColors = new Rgb24[colors.Length];
            alpha = new byte[colors.Length];

            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = colors[i].ToRgb24();
                alpha[i] = colors[i].Alpha;
            }
        }

        public static void SplitAlpha(Rgba64[] colors, out Rgb48[] newColors, out ushort[] alpha)
        {
            newColors = new Rgb48[colors.Length];
            alpha = new ushort[colors.Length];

            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = colors[i].ToRgb48();
                alpha[i] = colors[i].Alpha;
            }
        }

        public static void SplitAlpha(Color[] colors, out Color[] newColors, out byte[] alpha)
        {
            newColors = new Color[colors.Length];
            alpha = new byte[colors.Length];

            for (int i = 0; i < alpha.Length; i++)
            {
                newColors[i] = Color.FromArgb(255, colors[i]);
                alpha[i] = colors[i].A;
            }
        }

        #endregion

        #region Indexed Colors

        public static Rgb24[] GetIndexedColors(int[] indices, Rgb24[] palette)
        {
            Rgb24[] colors = new Rgb24[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                colors[i] = palette[indices[i]];
            }
            return colors;
        }

        public static Rgb48[] GetIndexedColors(int[] indices, Rgb48[] palette)
        {
            Rgb48[] colors = new Rgb48[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                colors[i] = palette[indices[i]];
            }
            return colors;
        }

        public static Rgba32[] GetIndexedColors(int[] indices, Rgba32[] palette)
        {
            Rgba32[] colors = new Rgba32[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                colors[i] = palette[indices[i]];
            }
            return colors;
        }

        public static Rgba64[] GetIndexedColors(int[] indices, Rgba64[] palette)
        {
            Rgba64[] colors = new Rgba64[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                colors[i] = palette[indices[i]];
            }
            return colors;
        }

        public static Color[] GetIndexedColors(int[] indices, Color[] palette, bool hasAlpha)
        {
            Color[] colors = new Color[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                colors[i] = hasAlpha ? palette[indices[i]] : Color.FromArgb(255, palette[indices[i]]);
            }
            return colors;
        }

        #endregion

        #region To Color

        public static Color[] ToColor(Rgb24[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(Rgb24[] colors, Rgb24 transparent)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                if (transparent.Red == color.Red
                    && transparent.Green == color.Green
                    && transparent.Blue == color.Blue)
                    newColors[i] = color.ToColor(0);
                else
                    newColors[i] = color.ToColor();
                
            }
            return newColors;
        }

        public static Color[] ToColor(Rgb48[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(Rgb48[] colors, Rgb48 transparent)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                if (transparent.Red == color.Red
                    && transparent.Green == color.Green
                    && transparent.Blue == color.Blue)
                    newColors[i] = color.ToColor(0);
                else
                    newColors[i] = color.ToColor();

            }
            return newColors;
        }

        public static Color[] ToColor(Rgba32[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(Rgba64[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(L8[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(L8[] colors, L8 transparent)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                if (color.Luminance == transparent.Luminance)
                    newColors[i] = color.ToColor(0);
                else
                    newColors[i] = color.ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(L16[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(L16[] colors, L16 transparent)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                if (color.Luminance == transparent.Luminance)
                    newColors[i] = color.ToColor(0);
                else
                    newColors[i] = color.ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(La16[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        public static Color[] ToColor(La32[] colors)
        {
            Color[] newColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = colors[i].ToColor();
            }
            return newColors;
        }

        #endregion

        #region From Color

        public static Rgb24[] ToRgb24(Color[] colors)
        {
            Rgb24[] newColors = new Rgb24[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = Rgb24.FromColor(colors[i]);
            }
            return newColors;
        }

        public static Rgb48[] ToRgb48(Color[] colors)
        {
            Rgb48[] newColors = new Rgb48[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = Rgb48.FromColor(colors[i]);
            }
            return newColors;
        }

        public static Rgba32[] ToRgba32(Color[] colors)
        {
            Rgba32[] newColors = new Rgba32[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = Rgba32.FromColor(colors[i]);
            }
            return newColors;
        }

        public static Rgba64[] ToRgba64(Color[] colors)
        {
            Rgba64[] newColors = new Rgba64[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = Rgba64.FromColor(colors[i]);
            }
            return newColors;
        }

        public static L8[] ToL8(Color[] colors)
        {
            L8[] newColors = new L8[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = L8.FromColor(colors[i]);
            }
            return newColors;
        }

        public static L16[] ToL16(Color[] colors)
        {
            L16[] newColors = new L16[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = L16.FromColor(colors[i]);
            }
            return newColors;
        }

        public static La16[] ToLa16(Color[] colors)
        {
            La16[] newColors = new La16[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = La16.FromColor(colors[i]);
            }
            return newColors;
        }

        public static La32[] ToLa32(Color[] colors)
        {
            La32[] newColors = new La32[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                newColors[i] = La32.FromColor(colors[i]);
            }
            return newColors;
        }

        #endregion

        #region From Channel

        public static Rgb24[] FromChannelBytesToRgb24(byte[] values)
        {
            if ((values.Length % 3) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgb24[] colors = new Rgb24[values.Length / 3];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgb24(values[i++], values[i++], values[i++]);
            }

            return colors;
        }

        public static Rgb48[] FromChannelBytesToRgb48(byte[] values)
        {
            if ((values.Length % 3) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgb48[] colors = new Rgb48[values.Length / 3];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgb48(values[i++], values[i++], values[i++]);
            }

            return colors;
        }

        public static Rgba32[] FromChannelBytesToRgba32(byte[] values)
        {
            if ((values.Length % 4) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgba32[] colors = new Rgba32[values.Length / 4];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgba32(values[i++], values[i++], values[i++], values[i++]);
            }

            return colors;
        }

        public static Rgba64[] FromChannelBytesToRgba64(byte[] values)
        {
            if ((values.Length % 4) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgba64[] colors = new Rgba64[values.Length / 4];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgba64(values[i++], values[i++], values[i++], values[i++]);
            }

            return colors;
        }

        public static Rgb24[] FromChannelIntsToRgb24(int[] values)
        {
            if ((values.Length % 3) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgb24[] colors = new Rgb24[values.Length / 3];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgb24((byte)values[i++], (byte)values[i++], (byte)values[i++]);
            }

            return colors;
        }

        public static Rgba32[] FromChannelIntsToRgba32(int[] values)
        {
            if ((values.Length % 4) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgba32[] colors = new Rgba32[values.Length / 4];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgba32((byte)values[i++], (byte)values[i++], (byte)values[i++], (byte)values[i++]);
            }

            return colors;
        }

        public static Rgb48[] FromChannelIntsToRgb48(int[] values)
        {
            if ((values.Length % 3) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgb48[] colors = new Rgb48[values.Length / 3];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgb48((ushort)values[i++], (ushort)values[i++], (ushort)values[i++]);
            }

            return colors;
        }

        public static Rgba64[] FromChannelIntsToRgba64(int[] values)
        {
            if ((values.Length % 4) != 0)
                throw new InvalidDataException($"There are not enough or too many values for each channel.");

            Rgba64[] colors = new Rgba64[values.Length / 4];
            int colorIndex = 0;
            for (int i = 0; i < values.Length;)
            {
                colors[colorIndex++] = new Rgba64((ushort)values[i++], (ushort)values[i++], (ushort)values[i++], (ushort)values[i++]);
            }

            return colors;
        }

        #endregion

        #region To Channel

        public static byte[] ToChannelBytes(Rgb24[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 3;
            var values = new byte[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
            }
            return values;
        }

        public static byte[] ToChannelBytes(Rgba32[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 4;
            var values = new byte[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
                values[i++] = color.Alpha;
            }
            return values;
        }

        public static byte[] ToChannelBytes(L8[] colors)
        {
            var values = new byte[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                values[i] = colors[i].Luminance;
            }
            return values;
        }

        public static byte[] ToChannelBytes(La16[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 2;
            var values = new byte[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Luminance;
                values[i++] = color.Alpha;
            }
            return values;
        }

        public static int[] ToChannelInts(Rgb24[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 3;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
            }
            return values;
        }

        public static int[] ToChannelInts(Rgb48[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 3;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
            }
            return values;
        }

        public static int[] ToChannelInts(Rgba32[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 4;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
                values[i++] = color.Alpha;
            }
            return values;
        }

        public static int[] ToChannelInts(Rgba64[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 4;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Red;
                values[i++] = color.Green;
                values[i++] = color.Blue;
                values[i++] = color.Alpha;
            }
            return values;
        }

        public static int[] ToChannelInts(L8[] colors)
        {
            var values = new int[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                values[i] = colors[i].Luminance;
            }
            return values;
        }

        public static int[] ToChannelInts(L16[] colors)
        {
            var values = new int[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                values[i] = colors[i].Luminance;
            }
            return values;
        }

        public static int[] ToChannelInts(La16[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 2;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Luminance;
                values[i++] = color.Alpha;
            }
            return values;
        }

        public static int[] ToChannelInts(La32[] colors)
        {
            int colorIndex = 0;
            int length = colors.Length * 2;
            var values = new int[length];
            for (int i = 0; i < length;)
            {
                var color = colors[colorIndex++];
                values[i++] = color.Luminance;
                values[i++] = color.Alpha;
            }
            return values;
        }

        #endregion

    }
}
