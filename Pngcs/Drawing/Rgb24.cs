using System.Drawing;

namespace Pngcs.Drawing
{
    public struct Rgb24
    {
        public byte Red;
        public byte Green;
        public byte Blue;

        public Rgb24(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        #region Convert

        public static Rgb24 FromColor(Color color)
            => new Rgb24(color.R, color.G, color.B);

        public Rgb48 ToRgb48()
            => new Rgb48(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue));

        public Rgba32 ToRgba32()
            => new Rgba32(Red, Green, Blue, byte.MaxValue);

        public Rgba32 ToRgba32(byte alpha)
            => new Rgba32(Red, Green, Blue, alpha);

        public Rgba64 ToRgba64()
            => new Rgba64(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue), ushort.MaxValue);

        public Rgba64 ToRgba64(ushort alpha)
            => new Rgba64(BitDim.ToUInt16(Red), BitDim.ToUInt16(Green), BitDim.ToUInt16(Blue), alpha);

        public Color ToColor()
            => Color.FromArgb(byte.MaxValue, Red, Green, Blue);

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, Red, Green, Blue);

        public L8 ToL8()
            => new L8(ColorDim.ToL8(Red, Green, Blue));

        public L16 ToL16()
            => new L16(ColorDim.ToL16(Red, Green, Blue));

        public La16 ToLa16()
            => new La16(ColorDim.ToL8(Red, Green, Blue), byte.MaxValue);

        public La16 ToLa16(byte alpha)
            => new La16(ColorDim.ToL8(Red, Green, Blue), alpha);

        public La32 ToLa32()
            => new La32(ColorDim.ToL16(Red, Green, Blue), ushort.MaxValue);

        public La32 ToLa32(ushort alpha)
            => new La32(ColorDim.ToL16(Red, Green, Blue), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"R={Red}, G={Green}, B={Blue}";

        #endregion
    }
}
