using System;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    /// A few utility static methods to read and write files.
    /// </summary>
    public static class PngFileHelper
    {
        /// <summary>
        /// Opens a <see cref="PngReader"/> with the specified file path.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A <see cref="PngReader"/>.</returns>
        public static PngReader PngOpenRead(string path)
            => new PngReader(File.OpenRead(path));

        /// <summary>
        /// Opens a <see cref="PngWriter"/> with the specified file path and <see cref="ImageInfo"/>.</summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="imgInfo">The <see cref="ImageInfo"/> to set.</param>
        /// <param name="allowOverwrite">Whether or not to allow overwriting, throwing when not allowed.</param>
        /// <returns>A <see cref="PngWriter"/>.</returns>
        public static PngWriter PngOpenWrite(string path, ImageInfo imgInfo, bool allowOverwrite)
        {
            if (File.Exists(path) && !allowOverwrite)
                throw new Exception($"File already exists and {nameof(allowOverwrite)} is false: {path}");

            return new PngWriter(File.OpenWrite(path), imgInfo);
        }
    }
}
