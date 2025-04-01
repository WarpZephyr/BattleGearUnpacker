using System.Drawing;

namespace Pngcs.Drawing
{
    public struct Rgba32
    {
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alpha;

        public Rgba32(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        #region Convert

        public static Rgba32 FromColor(Color color)
            => new Rgba32(color.R, color.G, color.B, color.A);

        public static Rgba32 FromColor(Color color, byte alpha)
            => new Rgba32(color.R, color.G, color.B, alpha);

        public Rgb24 ToRgb24()
            => new Rgb24(Red, Green, Blue);

        public Rgb48 ToRgb48()
            => new Rgb48(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue));

        public Rgba64 ToRgba64()
            => new Rgba64(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue), BitDim.ToUInt16(Alpha));

        public Rgba64 ToRgba64(ushort alpha)
            => new Rgba64(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue), alpha);

        public Color ToColor()
            => Color.FromArgb(Alpha, Red, Green, Blue);

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, Red, Green, Blue);

        public L8 ToL8()
            => new L8(ColorDim.ToL8(Red, Green, Blue));

        public L16 ToL16()
            => new L16(ColorDim.ToL16(Red, Green, Blue));

        public La16 ToLa16()
            => new La16(ColorDim.ToL8(Red, Green, Blue), Alpha);

        public La16 ToLa16(byte alpha)
            => new La16(ColorDim.ToL8(Red, Green, Blue), alpha);

        public La32 ToLa32()
            => new La32(ColorDim.ToL16(Red, Green, Blue), BitDim.ToUInt16(Alpha));

        public La32 ToLa32(ushort alpha)
            => new La32(ColorDim.ToL16(Red, Green, Blue), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"R={Red}, G={Green}, B={Blue}, A={Alpha}";

        #endregion
    }
}
