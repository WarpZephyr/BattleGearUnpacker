using BattleGearUnpacker.Core;
using BattleGearUnpacker.Core.Exceptions;
using BattleGearUnpacker.Formats;
using BattleGearUnpacker.Unpackers;

namespace BattleGearUnpacker
{
    internal class Program
    {
        /// <summary>
        /// The name of the program, used for identifying it.
        /// </summary>
        internal const string ProgramName = "BattleGearUnpacker";

        // Wrap main processing in a try catch for release builds.
        // For friendlier messages to users.
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
            ProcessArguments(args);
#if !DEBUG
            }
            catch (FriendlyException ex)
            {
               Console.WriteLine(ex.Message); 
            }
#endif
        }

        /// <summary>
        /// A function made so wrapping instructions from <see cref="Main(string[])"/> with a try catch was easier.
        /// </summary>
        /// <param name="args">The arguments of the program.</param>
        /// <exception cref="FriendlyException">A user friendly exception meant to be caught with only its message shown.</exception>
        static void ProcessArguments(string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }

            foreach (string path in args)
            {
                if (File.Exists(path))
                {
                    if (path.EndsWith("BG3ZPACK.ARC"))
                    {
                        Console.WriteLine("Unpacking BG3ZPACK...");
                        string? folder = Path.GetDirectoryName(path) ?? throw new FriendlyException($"Could not get folder path of: {path}");
                        string headerPath = Path.Combine(folder, "FAT_Z.BIN");
                        if (!File.Exists(headerPath))
                            throw new FriendlyException($"Could not find header path at: {headerPath}");

                        string outFolderName = Path.GetFileName(path).Replace('.', '-');
                        string outFolder = Path.Combine(folder, outFolderName);

                        var cpb = new ConsoleProgressBar();
                        ZPACKUnpacker.Unpack(headerPath, path, outFolder, cpb);
                        cpb.Dispose();
                        Console.Write(" Done.\n");
                    }
                    else if (path.EndsWith("FAT_Z.BIN"))
                    {
                        Console.WriteLine("Unpacking BG3ZPACK...");
                        string? folder = Path.GetDirectoryName(path) ?? throw new FriendlyException($"Could not get folder path of: {path}");
                        string dataPath = Path.Combine(folder, "BG3ZPACK.ARC");
                        if (!File.Exists(dataPath))
                            throw new FriendlyException($"Could not find data path at: {dataPath}");

                        string outFolderName = Path.GetFileName(dataPath).Replace('.', '-');
                        string outFolder = Path.Combine(folder, outFolderName);

                        var cpb = new ConsoleProgressBar();
                        ZPACKUnpacker.Unpack(path, dataPath, outFolder, cpb);
                        cpb.Dispose();
                        Console.Write(" Done.\n");
                    }
                    else if (path.EndsWith(".GST"))
                    {
                        Console.WriteLine("Decompressing GST...");
                        GST.DecompressTo(path, path + ".DE");
                    }
                    else if (path.EndsWith(".GST.DE"))
                    {
                        Console.WriteLine("Compressing GST...");
                        GST.CompressTo(path, Path.GetFileNameWithoutExtension(path));
                    }
                    else if (path.EndsWith(".FOZ"))
                    {
                        Console.WriteLine("Unpacking FOZ...");
                        string outFolder = Path.GetDirectoryName(path) ?? throw new FriendlyException($"Could not get folder path of: {path}");
                        FOZ.DecompressTo(path, outFolder);
                    }
                    else if (BG3TIM2.IsRead(path, out BG3TIM2? file))
                    {
                        Console.WriteLine("Unpacking BG3 TIM2...");
                        BG3TIM2Unpacker.Unpack(path, file);
                    }
                    else if (path.EndsWith("_tim2.xml", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Repacking TIM2...");

                        // Keep support for generic TIM2 repack for now
                        TIM2Unpacker.Repack(path);
                    }
                    else if (path.EndsWith("_bg3tim2.xml", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Repacking BG3 TIM2...");
                        BG3TIM2Unpacker.Repack(path);
                    }
                    else if (path.EndsWith("_bg3zpack.xml", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Repacking BG3ZPACK...");

                        string? inFolder = Path.GetDirectoryName(path) ?? throw new FriendlyException($"Could not get folder path of: {path}");
                        string? outFolder = Path.GetDirectoryName(inFolder) ?? throw new FriendlyException($"Could not get folder path of: {inFolder}");
                        var cpb = new ConsoleProgressBar();
                        ZPACKUnpacker.Repack(inFolder, outFolder, cpb);
                        cpb.Dispose();
                        Console.Write(" Done.\n");
                    }
                }
                else if (Directory.Exists(path))
                {
                    if (File.Exists(Path.Combine(path, "_bg3zpack.xml")))
                    {
                        Console.WriteLine("Repacking BG3ZPACK...");

                        string? outFolder = Path.GetDirectoryName(path) ?? throw new FriendlyException($"Could not get folder path of: {path}");
                        var cpb = new ConsoleProgressBar();
                        ZPACKUnpacker.Repack(path, outFolder, cpb);
                        cpb.Dispose();
                        Console.Write(" Done.\n");
                    }
                }
            }

            Console.WriteLine("Finished.");
        }
    }
}
