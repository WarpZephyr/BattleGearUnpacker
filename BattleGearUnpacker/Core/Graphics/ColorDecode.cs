using System.Drawing;

namespace BattleGearUnpacker.Core.Graphics
{
    internal static class ColorDecode
    {
        public static Color FromA1B5G5R5(ushort value)
        {
            int alpha = ((value & 0b1_00000_00000_00000) >> 15) * 255;
            int blue = ((value & 0b0_11111_00000_00000) >> 10) * 8;
            int green = ((value & 0b0_00000_11111_00000) >> 5) * 8;
            int red = (value & 0b0_00000_00000_11111) * 8;
            return Color.FromArgb(alpha, red, green, blue);
        }

        public static Color FromA1B5G5R5(int value)
        {
            int alpha = ((value & 0b1_00000_00000_00000) >> 15) * 255;
            int blue = ((value & 0b0_11111_00000_00000) >> 10) * 8;
            int green = ((value & 0b0_00000_11111_00000) >> 5) * 8;
            int red = (value & 0b0_00000_00000_11111) * 8;
            return Color.FromArgb(alpha, red, green, blue);
        }

        public static ushort ToA1B5G5R5(Color color)
            => (ushort)((color.A / 255) << 15 | (color.B / 8) << 10 | (color.G / 8) << 5 | (color.R / 8));
    }
}
