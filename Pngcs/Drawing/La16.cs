using System.Drawing;

namespace Pngcs.Drawing
{
    public struct La16
    {
        public byte Luminance;
        public byte Alpha;

        public La16(byte luminance, byte alpha)
        {
            Luminance = luminance;
            Alpha = alpha;
        }

        #region Convert

        public static La16 FromColor(Color color)
            => new La16(ColorDim.ToL8(color.R, color.G, color.B), color.A);

        public static La16 FromColor(Color color, byte alpha)
            => new La16(ColorDim.ToL8(color.R, color.G, color.B), alpha);

        public Rgb24 ToRgb24()
            => new Rgb24(Luminance, Luminance, Luminance);

        public Rgb48 ToRgb48()
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgb48(luminance, luminance, luminance);
        }

        public Rgba32 ToRgba32()
            => new Rgba32(Luminance, Luminance, Luminance, Alpha);

        public Rgba32 ToRgba32(byte alpha)
            => new Rgba32(Luminance, Luminance, Luminance, alpha);

        public Rgba64 ToRgba64()
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgba64(luminance, luminance, luminance, BitDim.ToUInt16(Alpha));
        }

        public Rgba64 ToRgba64(ushort alpha)
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgba64(luminance, luminance, luminance, alpha);
        }

        public Color ToColor()
            => Color.FromArgb(Alpha, Luminance, Luminance, Luminance);

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, Luminance, Luminance, Luminance);

        public L8 ToL8()
            => new L8(Luminance);

        public L16 ToL16()
            => new L16(BitDim.ToUInt16(Luminance));

        public La32 ToLa32()
            => new La32(BitDim.ToUInt16(Luminance), BitDim.ToUInt16(Alpha));

        public La32 ToLa32(ushort alpha)
            => new La32(BitDim.ToUInt16(Luminance), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"L={Luminance}, A={Alpha}";

        #endregion
    }
}
