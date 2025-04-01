using System.Drawing;

namespace Pngcs.Drawing
{
    public struct Rgb48
    {
        public ushort Red;
        public ushort Green;
        public ushort Blue;

        public Rgb48(ushort red, ushort green, ushort blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        #region Convert

        public static Rgb48 FromColor(Color color)
            => new Rgb48(BitDim.ToUInt16(color.R), BitDim.ToUInt16(color.G), BitDim.ToUInt16(color.B));

        public Rgb24 ToRgb24()
            => new Rgb24(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

        public Rgba32 ToRgba32()
            => new Rgba32(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue), byte.MaxValue);

        public Rgba32 ToRgba32(byte alpha)
            => new Rgba32(BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue), alpha);

        public Rgba64 ToRgba64()
            => new Rgba64(Red, Green, Blue, ushort.MaxValue);

        public Rgba64 ToRgba64(ushort alpha)
            => new Rgba64(Red, Green, Blue, alpha);

        public Color ToColor()
            => Color.FromArgb(byte.MaxValue, BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, BitDim.ToByte(Red), BitDim.ToByte(Green), BitDim.ToByte(Blue));

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
