using System.Drawing;

namespace Pngcs.Drawing
{
    public struct L16
    {
        public ushort Luminance { get; set; }

        public L16(ushort luminance)
        {
            Luminance = luminance;
        }

        #region Convert

        public static L16 FromColor(Color color)
            => new L16(ColorDim.ToL16(color.R, color.G, color.B));

        public Rgb24 ToRgb24()
        {
            byte luminance = BitDim.ToByte(Luminance);
            return new Rgb24(luminance, luminance, luminance);
        }

        public Rgb48 ToRgb48()
            => new Rgb48(Luminance, Luminance, Luminance);

        public Rgba32 ToRgba32()
        {
            byte luminance = BitDim.ToByte(Luminance);
            return new Rgba32(luminance, luminance, luminance, byte.MaxValue);
        }

        public Rgba32 ToRgba32(byte alpha)
        {
            byte luminance = BitDim.ToByte(Luminance);
            return new Rgba32(luminance, luminance, luminance, alpha);
        }

        public Rgba64 ToRgba64()
            => new Rgba64(Luminance, Luminance, Luminance, ushort.MaxValue);

        public Rgba64 ToRgba64(ushort alpha)
            => new Rgba64(Luminance, Luminance, Luminance, alpha);

        public Color ToColor()
        {
            byte luminance = BitDim.ToByte(Luminance);
            return Color.FromArgb(byte.MaxValue, luminance, luminance, luminance);
        }

        public Color ToColor(byte alpha)
        {
            byte luminance = BitDim.ToByte(Luminance);
            return Color.FromArgb(alpha, luminance, luminance, luminance);
        }

        public L8 ToL8()
            => new L8(BitDim.ToByte(Luminance));

        public La16 ToLa16()
            => new La16(BitDim.ToByte(Luminance), byte.MaxValue);

        public La16 ToLa16(byte alpha)
            => new La16(BitDim.ToByte(Luminance), alpha);

        public La32 ToLa32()
            => new La32(Luminance, ushort.MaxValue);

        public La32 ToLa32(ushort alpha)
            => new La32(Luminance, alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"L={Luminance}";

        #endregion
    }
}
