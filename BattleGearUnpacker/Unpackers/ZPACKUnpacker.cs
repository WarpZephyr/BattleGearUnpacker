using BattleGearUnpacker.Core.Exceptions;
using BattleGearUnpacker.Core.Parsing.Xml;
using BattleGearUnpacker.Formats;
using System.Xml;

namespace BattleGearUnpacker.Unpackers
{
    public static class ZPACKUnpacker
    {
        public static void Unpack(string headerPath, string dataPath, string outFolder, IProgress<double> progress)
        {
            Directory.CreateDirectory(outFolder);

            var xws = new XmlWriterSettings
            {
                Indent = true
            };

            var xw = XmlWriter.Create(Path.Combine(outFolder, "_bg3zpack.xml"), xws);
            Dictionary<string, int> copyDictionary = [];
            xw.WriteStartElement("bg3zpack");
            xw.WriteElementString("decoder", Program.ProgramName);
            xw.WriteElementString("headername", Path.GetFileName(headerPath));
            xw.WriteElementString("dataname", Path.GetFileName(dataPath));
            xw.WriteStartElement("entries");
            using var reader = ZPACKReader.Read(headerPath, dataPath);
            double fileCount = reader.FileEntries.Length;
            int fileNum = 1;
            for (int i = 0; i < reader.FileEntries.Length; i++)
            {
                var entry = reader.FileEntries[i];
                if (!entry.IsEmpty)
                {
                    xw.WriteStartElement("entry");
                    string name = entry.Name;

                    xw.WriteElementString("name", entry.Name);
                    // There are duplicate names, and no longer any folder structure concerning them.
                    // This will store how many copies of an entry were found.
                    // I could dynamically check if the file already exists, but I want to overwrite previously extracted files.
                    // I would be duplicating every file the next extraction otherwise.
                    if (copyDictionary.TryGetValue(name, out int count))
                    {
                        copyDictionary[name] = count + 1;

                        string extension = Path.GetExtension(name);
                        name = $"{Path.GetFileNameWithoutExtension(name)} ({count}){extension}";

                        // Write to find duplicate name in folder
                        xw.WriteElementString("filename", name);
                    }
                    else
                    {
                        copyDictionary.Add(name, 1);
                    }

                    // Write after the potential duplicate name value
                    xw.WriteElementString("unk10", $"{entry.Unk10}");

                    string outPath = Path.Combine(outFolder, name);
                    using var output = File.Create(outPath);
                    entry.GetStream(output);
                    output.Flush();
                    xw.WriteEndElement();

                    progress.Report(fileNum / fileCount);
                    fileNum++;
                }
                else
                {
                    // Don't write empty entries to XML, we already know the allocation count.
                    break;
                }
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.Close();

            progress.Report(1);
        }

        public static void Repack(string inFolder, string outFolder, IProgress<double> progress)
        {
            var xml = new XmlDocument();
            xml.Load(Path.Combine(inFolder, "_bg3zpack.xml"));

            string decoder = xml.ReadStringOrDefault("bg3zpack/decoder", string.Empty);
            if (decoder != Program.ProgramName)
                Console.WriteLine($"Unrecognized decoder: {decoder}");

            string headerName = xml.ReadStringOrDefault("bg3zpack/headername", "FAT_Z.BIN");
            string dataName = xml.ReadStringOrDefault("bg3zpack/dataname", "BG3ZPACK.ARC");
            string headerPath = Path.Combine(outFolder, headerName);
            string dataPath = Path.Combine(outFolder, dataName);
            Util.Backup(headerPath);
            Util.Backup(dataPath);

            using var writer = ZPACKWriter.Create(headerPath, dataPath);
            var entriesNode = xml.SelectNodes("bg3zpack/entries/entry");
            if (entriesNode != null)
            {
                double fileCount = entriesNode.Count;
                int fileNum = 1;
                foreach (XmlNode entryNode in entriesNode)
                {
                    string entryName = entryNode.ReadString("name");
                    string fileName = entryNode.ReadStringOrDefault("filename", entryName);
                    short unk10 = entryNode.ReadInt16OrDefault("unk10", 0);
                    var entry = ZPACKWriter.CreateFileEntry(entryName);
                    entry.Unk10 = unk10;

                    string entryFilePath = Path.Combine(inFolder, fileName);
                    if (!File.Exists(entryFilePath))
                        throw new FriendlyException($"Cannot find a file specified for repacking: {entryFilePath}");

                    writer.WriteFile(entryFilePath, entry);
                    progress.Report(fileNum / fileCount);
                    fileNum++;
                }

                writer.Finish();
            }

            progress.Report(1);
        }
    }
}
