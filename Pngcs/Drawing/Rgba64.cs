using System.Drawing;

namespace Pngcs.Drawing
{
    public struct Rgba64
    {
        public ushort Red;
        public ushort Green;
        public ushort Blue;
        public ushort Alpha;

        public Rgba64(ushort red, ushort green, ushort blue, ushort alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        #region Convert

        public static Rgba64 FromColor(Color color)
            => new Rgba64(BitDim.ToUInt16(color.R), BitDim.ToUInt16(color.G), BitDim.ToUInt16(color.B), BitDim.ToUInt16(color.A));

        public static Rgba64 FromColor(Color color, ushort alpha)
            => new Rgba64(BitDim.ToUInt16(color.R), BitDim.ToUInt16(color.G), BitDim.ToUInt16(color.B), alpha);

        public Rgb24 ToRgb24()
            => new Rgb24(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

        public Rgb48 ToRgb48()
            => new Rgb48(Red, Green, Blue);

        public Rgba32 ToRgba32()
            => new Rgba32(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue), BitDim.ToByte(Alpha));

        public Rgba32 ToRgba32(byte alpha)
            => new Rgba32(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue), alpha);

        public Color ToColor()
            => Color.FromArgb(BitDim.ToByte(Alpha), BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

        public L8 ToL8()
            => new L8(ColorDim.ToL8(Red, Green, Blue));

        public L16 ToL16()
            => new L16(ColorDim.ToL16(Red, Green, Blue));

        public La16 ToLa16()
            => new La16(ColorDim.ToL8(Red, Green, Blue), BitDim.ToByte(Alpha));

        public La16 ToLa16(byte alpha)
            => new La16(ColorDim.ToL8(Red, Green, Blue), alpha);

        public La32 ToLa32()
            => new La32(ColorDim.ToL16(Red, Green, Blue), Alpha);

        public La32 ToLa32(ushort alpha)
            => new La32(ColorDim.ToL16(Red, Green, Blue), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"R={Red}, G={Green}, B={Blue}, A={Alpha}";

        #endregion
    }
}
