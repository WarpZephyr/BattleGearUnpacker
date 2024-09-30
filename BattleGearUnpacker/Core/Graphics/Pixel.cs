using System.Drawing;

namespace BattleGearUnpacker.Core.Graphics
{
    public struct Pixel
    {
        public Color Color { get; set; }
        public int Index { get; set; }

        public Pixel(Color color, int index)
        {
            Color = color;
            Index = index;
        }

        public Pixel(Color color)
        {
            Color = color;
            Index = -1;
        }

        public Pixel(int index)
        {
            Color = Color.FromArgb(0);
            Index = index;
        }
    }
}
