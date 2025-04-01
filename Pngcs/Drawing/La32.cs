using System.Drawing;

namespace Pngcs.Drawing
{
    public struct La32
    {
        public ushort Luminance;
        public ushort Alpha;

        public La32(ushort luminance, ushort alpha)
        {
            Luminance = luminance;
            Alpha = alpha;
        }

        #region Convert

        public static La32 FromColor(Color color)
            => new La32(ColorDim.ToL16(color.R, color.G, color.B), color.A);

        public static La32 FromColor(Color color, ushort alpha)
            => new La32(ColorDim.ToL16(color.R, color.G, color.B), alpha);

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
            return new Rgba32(luminance, luminance, luminance, BitDim.ToByte(Alpha));
        }

        public Rgba32 ToRgba32(byte alpha)
        {
            byte luminance = BitDim.ToByte(Luminance);
            return new Rgba32(luminance, luminance, luminance, alpha);
        }

        public Rgba64 ToRgba64()
            => new Rgba64(Luminance, Luminance, Luminance, Alpha);

        public Rgba64 ToRgba64(ushort alpha)
            => new Rgba64(Luminance, Luminance, Luminance, alpha);

        public Color ToColor()
        {
            byte luminance = BitDim.ToByte(Luminance);
            return Color.FromArgb(BitDim.ToByte(Alpha), luminance, luminance, luminance);
        }

        public Color ToColor(byte alpha)
        {
            byte luminance = BitDim.ToByte(Luminance);
            return Color.FromArgb(alpha, luminance, luminance, luminance);
        }

        public L8 ToL8()
            => new L8(BitDim.ToByte(Luminance));

        public L16 ToL16()
            => new L16(Luminance);

        public La16 ToLa16()
            => new La16(BitDim.ToByte(Luminance), BitDim.ToByte(Alpha));

        public La16 ToLa16(byte alpha)
            => new La16(BitDim.ToByte(Luminance), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"L={Luminance}, A={Alpha}";

        #endregion
    }
}
