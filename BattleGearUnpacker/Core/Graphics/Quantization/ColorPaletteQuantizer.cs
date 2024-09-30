using System.Drawing;

namespace BattleGearUnpacker.Core.Graphics.Quantization
{
    internal static class ColorPaletteQuantizer
    {
        public static Pixel[] Quantize(Pixel[] pixels, Color[] palette)
        {
            Pixel[] newPixels = new Pixel[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                newPixels[i] = new Pixel(pixels[i].Color, Quantize(pixels[i].Color, palette));
            }

            return newPixels;
        }

        // https://learn.microsoft.com/en-us/previous-versions/dotnet/articles/aa479306(v=msdn.10)#palette-based-quantization
        public static int Quantize(Color pixel, Color[] palette)
        {
            int colorIndex = -1;
            int leastDistance = int.MaxValue;
            byte red = pixel.R;
            byte green = pixel.G;
            byte blue = pixel.B;
            byte alpha = pixel.A;

            // Loop through the entire palette, looking for the closest color match
            for (int index = 0; index < palette.Length; index++)
            {
                // Lookup the color from the palette
                Color paletteColor = palette[index];

                // Compute the distance from our source color to the palette color
                int redDistance = paletteColor.R - red;
                int greenDistance = paletteColor.G - green;
                int blueDistance = paletteColor.B - blue;
                int alphaDistance = paletteColor.A - alpha;

                int distance = (redDistance * redDistance) +
                                    (greenDistance * greenDistance) +
                                    (blueDistance * blueDistance) +
                                    (alphaDistance * alphaDistance);

                // If the color is closer than any other found so far, use it
                if (distance < leastDistance)
                {
                    colorIndex = index;
                    leastDistance = distance;

                    // And if it's an exact match, exit the loop
                    if (0 == distance)
                        break;
                }
            }

            return colorIndex;
        }
    }
}
