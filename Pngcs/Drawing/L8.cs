using System.Drawing;

namespace Pngcs.Drawing
{
    public struct L8
    {
        public byte Luminance;

        public L8(byte luminance)
        {
            Luminance = luminance;
        }

        #region Convert

        public static L8 FromColor(Color color)
            => new L8(ColorDim.ToL8(color.R, color.G, color.B));

        public Rgb24 ToRgb24()
            => new Rgb24(Luminance, Luminance, Luminance);

        public Rgb48 ToRgb48()
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgb48(luminance, luminance, luminance);
        }

        public Rgba32 ToRgba32()
            => new Rgba32(Luminance, Luminance, Luminance, byte.MaxValue);

        public Rgba32 ToRgba32(byte alpha)
            => new Rgba32(Luminance, Luminance, Luminance, alpha);

        public Rgba64 ToRgba64()
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgba64(luminance, luminance, luminance, ushort.MaxValue);
        }

        public Rgba64 ToRgba64(ushort alpha)
        {
            ushort luminance = BitDim.ToUInt16(Luminance);
            return new Rgba64(luminance, luminance, luminance, alpha);
        }

        public Color ToColor()
            => Color.FromArgb(byte.MaxValue, Luminance, Luminance, Luminance);

        public Color ToColor(byte alpha)
            => Color.FromArgb(alpha, Luminance, Luminance, Luminance);

        public L16 ToL16()
            => new L16(BitDim.ToUInt16(Luminance));

        public La16 ToLa16()
            => new La16(Luminance, byte.MaxValue);

        public La32 ToLa32()
            => new La32(BitDim.ToUInt16(Luminance), ushort.MaxValue);

        public La32 ToLa32(ushort alpha)
            => new La32(BitDim.ToUInt16(Luminance), alpha);

        #endregion

        #region ToString

        public override readonly string ToString()
            => $"L={Luminance}";

        #endregion
    }
}
