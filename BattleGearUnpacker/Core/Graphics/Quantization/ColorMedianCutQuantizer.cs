using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleGearUnpacker.Core.Graphics.Quantization
{
    // https://github.com/bacowan/cSharpColourQuantization
    internal class ColorMedianCutQuantizer
    {
        public static Pixel[] Quantize(Pixel[] pixels, int colorCount, out Color[] palette)
        {
            // Extract colors
            Color[] colors = new Color[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                colors[i] = pixels[i].Color;
            }

            // Quantize
            var buckets = new List<Bucket>
            {
                new Bucket(colors)
            };

            while (buckets.Count < colorCount)
            {
                var newBuckets = new List<Bucket>();
                for (var i = 0; i < buckets.Count; i++)
                {
                    if (newBuckets.Count + (buckets.Count - i) < colorCount)
                    {
                        var split = buckets[i].Split();
                        newBuckets.Add(split.Item1);
                        newBuckets.Add(split.Item2);
                    }
                    else
                    {
                        newBuckets.AddRange(buckets.GetRange(i, buckets.Count - i));
                        break;
                    }
                }
                buckets = newBuckets;
            }

            // Get quantized image and palette
            int colorIndex = 0;
            var colorDict = new Dictionary<Color, int>();
            var result = new Pixel[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var bucket = buckets.First(b => b.HasColor(pixels[i].Color));
                if (colorDict.TryAdd(bucket.Color, colorIndex))
                {
                    result[i] = new Pixel(bucket.Color, colorIndex);
                    colorIndex++;
                }
                else
                {
                    result[i] = new Pixel(bucket.Color, colorDict[bucket.Color]);
                }
                
            }

            palette = [.. colorDict.Keys];
            return result;
        }

        private class Bucket
        {
            private readonly Dictionary<Color, int> Colors;

            public Color Color { get; }

            public Bucket(Color[] colors)
            {
                Colors = colors.ToLookup(c => c).ToDictionary(c => c.Key, c => c.Count());
                Color = Average(Colors);
            }

            public Bucket(IEnumerable<KeyValuePair<Color, int>> enumerable)
            {
                Colors = enumerable.ToDictionary(c => c.Key, c => c.Value);
                Color = Average(Colors);
            }

            private static Color Average(IEnumerable<KeyValuePair<Color, int>> colors)
            {
                var totals = colors.Sum(c => c.Value);
                return Color.FromArgb(
                    alpha: 255,
                    red: colors.Sum(c => c.Key.R * c.Value) / totals,
                    green: colors.Sum(c => c.Key.G * c.Value) / totals,
                    blue: colors.Sum(c => c.Key.B * c.Value) / totals);
            }

            public bool HasColor(Color color)
            {
                return Colors.ContainsKey(color);
            }

            public Tuple<Bucket, Bucket> Split()
            {
                var redRange = Colors.Keys.Max(c => c.R) - Colors.Keys.Min(c => c.R);
                var greenRange = Colors.Keys.Max(c => c.G) - Colors.Keys.Min(c => c.G);
                var blueRange = Colors.Keys.Max(c => c.B) - Colors.Keys.Min(c => c.B);

                Func<Color, int> sorter;
                if (redRange > greenRange)
                {
                    sorter = c => c.R;
                }
                else if (greenRange > blueRange)
                {
                    sorter = c => c.G;
                }
                else
                {
                    sorter = c => c.B;
                }

                var sorted = Colors.OrderBy(c => sorter(c.Key));

                var firstBucketCount = sorted.Count() / 2;

                var bucket1 = new Bucket(sorted.Take(firstBucketCount));
                var bucket2 = new Bucket(sorted.Skip(firstBucketCount));
                return new Tuple<Bucket, Bucket>(bucket1, bucket2);
            }
        }
    }
}
