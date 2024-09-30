using BattleGearUnpacker.Core.Graphics;
using BattleGearUnpacker.Formats;
using static BattleGearUnpacker.Formats.BG3TIM2;
using System.Drawing;
using System.Xml;
using BattleGearUnpacker.Core.Parsing.Xml;
using BattleGearUnpacker.Core.Exceptions;

namespace BattleGearUnpacker.Unpackers
{
    public static class BG3TIM2Unpacker
    {
        public static void Unpack(string path, BG3TIM2 file)
        {
            string filename = Path.GetFileName(path);
            string extensionless = Path.GetFileNameWithoutExtension(filename);
            string? folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
                throw new FriendlyException($"Could not get folder path of: {path}");

            var xws = new XmlWriterSettings
            {
                Indent = true
            };

            bool indexName = file.Pictures.Count > 1;
            var xw = XmlWriter.Create(Path.Combine(folder, $"{extensionless}_bg3tim2.xml"), xws);
            xw.WriteStartElement("bg3tim2");
            xw.WriteElementString("decoder", Program.ProgramName);
            xw.WriteElementString("filename", filename);
            xw.WriteElementString("formatversion", $"{file.FormatVersion}");
            xw.WriteElementString("formatid", $"{(byte)file.FormatID}");
            xw.WriteStartElement("pictures");
            for (int picIndex = 0; picIndex < file.Pictures.Count; picIndex++)
            {
                string outName = indexName ? $"{extensionless}{picIndex}.png" : $"{extensionless}.png";
                string picOutPath = Path.Combine(folder, outName);
                var picture = file.Pictures[picIndex];

                // Write PNG
                bool indexed = picture.Indexed;
                bool hasAlpha = picture.HasAlpha;
                int bitDepth = indexed ? picture.BitDepth : 8;
                ImageUtil.WritePNG(picOutPath, picture.Width, picture.Height, bitDepth, hasAlpha, indexed, picture.Image, picture.Clut);

                xw.WriteStartElement("picture");
                xw.WriteElementString("filename", outName);
                xw.WriteElementString("clutcolors", $"{picture.Clut.Length}");
                xw.WriteElementString("pictformat", $"{picture.PictureFormat}");
                xw.WriteElementString("mipmaptextures", $"{1 + picture.Mipmaps.Count}");
                xw.WriteElementString("imagetype", $"{Picture.GetColorTypeName(picture.ImageColorType)}");
                xw.WriteElementString("width", $"{picture.Width}");
                xw.WriteElementString("height", $"{picture.Height}");
                xw.WriteElementString("extendedheader", $"{picture.WriteExtendedHeader}");
                xw.WriteElementString("comment", picture.Comment);

                // Whether or not to set alpha to 0x80, 128, or 50%
                // Sometimes images say they don't have alpha in GsTex0, but are using RGB32 (RGBA) storage, the alpha channel is set to 0x80 in this.
                xw.WriteElementString("correctalpha",
                    $"{(picture.Indexed ? picture.ClutType.ClutColorType : picture.ImageColorType) == Picture.ColorType.RGB32
                    && picture.GsTex.TextureColorComponent == Picture.TextureColorComponentType.RGB}");

                xw.WriteStartElement("cluttype");
                xw.WriteElementString("clutcolortype", $"{Picture.GetColorTypeName(picture.ClutType.ClutColorType)}");
                xw.WriteElementString("clutcompound", $"{picture.ClutType.ClutCompound}");
                xw.WriteElementString("csm", $"{picture.ClutType.ClutStorageMode}");
                xw.WriteEndElement();

                xw.WriteStartElement("gstex0");
                xw.WriteElementString("tbp0", $"{picture.GsTex.TextureBasePointer}");
                xw.WriteElementString("tbw", $"{picture.GsTex.TextureBufferWidth}");
                xw.WriteElementString("psm", $"{picture.GsTex.PixelStorageMode}");
                xw.WriteElementString("tw", $"{picture.GsTex.TextureWidth}");
                xw.WriteElementString("th", $"{picture.GsTex.TextureHeight}");
                xw.WriteElementString("tcc", $"{picture.GsTex.TextureColorComponent}");
                xw.WriteElementString("tfx", $"{picture.GsTex.TextureFunction}");
                xw.WriteElementString("cbp", $"{picture.GsTex.ClutBasePointer}");
                xw.WriteElementString("cpsm", $"{picture.GsTex.ClutPixelStorageMode}");
                xw.WriteElementString("csm", $"{picture.GsTex.ClutStorageMode}");
                xw.WriteElementString("csa", $"{picture.GsTex.ClutStartAddress}");
                xw.WriteElementString("cld", $"{picture.GsTex.ClutLoadControl}");
                xw.WriteEndElement();

                xw.WriteStartElement("gstex1");
                xw.WriteElementString("lcm", $"{picture.GsTex.LODCalculationMethod}");
                xw.WriteElementString("mxl", $"{picture.GsTex.MipLevelMax}");
                xw.WriteElementString("mmag", $"{picture.GsTex.MipMag}");
                xw.WriteElementString("mmin", $"{picture.GsTex.MipMin}");
                xw.WriteElementString("mtba", $"{picture.GsTex.MipmapTextureBaseAddress}");
                xw.WriteElementString("l", $"{picture.GsTex.LODParameterL}");
                xw.WriteElementString("k", $"{picture.GsTex.LODParameterK}");
                xw.WriteEndElement();

                xw.WriteStartElement("gstexafbapabe");
                xw.WriteElementString("ta0", $"{picture.GsTex.TA0}");
                xw.WriteElementString("aem", $"{picture.GsTex.AEM}");
                xw.WriteElementString("ta1", $"{picture.GsTex.TA1}");
                xw.WriteElementString("pabe", $"{picture.GsTex.PABE}");
                xw.WriteElementString("fba", $"{picture.GsTex.FBA}");
                xw.WriteEndElement();

                xw.WriteStartElement("gstexclut");
                xw.WriteElementString("cbw", $"{picture.GsTex.ClutBufferWidth}");
                xw.WriteElementString("cou", $"{picture.GsTex.ClutOffsetU}");
                xw.WriteElementString("cov", $"{picture.GsTex.ClutOffsetV}");
                xw.WriteEndElement();

                if (picture.Mipmaps.Count > 0)
                {
                    xw.WriteStartElement("mipmaps");
                    xw.WriteStartElement("gsmiptbp1");
                    for (int i = 0; i < 3; i++)
                    {
                        int level = i + 1;
                        xw.WriteElementString($"tbp{level}", $"{picture.MipmapTextureBasePointers[i]}");
                        xw.WriteElementString($"tbw{level}", $"{picture.MipmapTextureBufferWidths[i]}");
                    }
                    xw.WriteEndElement();

                    xw.WriteStartElement("gsmiptbp2");
                    for (int i = 3; i < 6; i++)
                    {
                        int level = i + 1;
                        xw.WriteElementString($"tbp{level}", $"{picture.MipmapTextureBasePointers[i]}");
                        xw.WriteElementString($"tbw{level}", $"{picture.MipmapTextureBufferWidths[i]}");
                    }
                    xw.WriteEndElement();

                    // Write PNGs
                    xw.WriteStartElement("filenames");
                    for (int mipIndex = 0; mipIndex < picture.Mipmaps.Count; mipIndex++)
                    {
                        int level = mipIndex + 1;
                        outName = indexName ? $"{extensionless}{picIndex}_LV{level}.png" : $"{extensionless}_LV{level}.png";
                        picOutPath = Path.Combine(folder, outName);
                        ImageUtil.WritePNG(picOutPath, picture.Width >>> level, picture.Height >>> level, bitDepth, hasAlpha, indexed, picture.Mipmaps[mipIndex], picture.Clut);
                        xw.WriteElementString("filename", outName);
                    }
                    xw.WriteEndElement();
                    xw.WriteEndElement();
                }

                if (picture.SubImages.Count > 0)
                {
                    xw.WriteStartElement("subimages");
                    foreach (var subimage in picture.SubImages)
                    {
                        xw.WriteStartElement("subimage");
                        xw.WriteElementString("name", subimage.Name);
                        xw.WriteElementString("width", $"{subimage.Width}");
                        xw.WriteElementString("height", $"{subimage.Height}");
                        xw.WriteElementString("x", $"{subimage.X}");
                        xw.WriteElementString("y", $"{subimage.Y}");
                        xw.WriteEndElement();
                    }
                    xw.WriteEndElement();
                }

                if (indexed)
                {
                    xw.WriteStartElement("clut");
                    for (int i = 0; i < picture.Clut.Length; i++)
                    {
                        var color = picture.Clut[i];
                        xw.WriteElementString("color", $"[{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}]");
                    }
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
            xw.Close();
        }

        public static void Repack(string path)
        {
            string? folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
                throw new FriendlyException($"Could not get folder path of: {path}");

            var file = new BG3TIM2();
            var xml = new XmlDocument();
            xml.Load(path);
            string decoder = xml.ReadStringOrDefault("bg3tim2/decoder", string.Empty);
            if (decoder != Program.ProgramName)
                Console.WriteLine($"Unrecognized decoder: {decoder}");

            string outName = xml.ReadString("bg3tim2/filename");
            file.FormatVersion = xml.ReadByte("bg3tim2/formatversion");
            file.FormatID = (FormatAlignment)xml.ReadByte("bg3tim2/formatid");

            var picturesNode = xml.SelectNodes("bg3tim2/pictures/picture");
            if (picturesNode != null)
            {
                foreach (XmlNode pictureNode in picturesNode)
                {
                    string pngName = pictureNode.ReadString("filename");
                    string pngPath = Path.Combine(folder, pngName);
                    if (!File.Exists(pngPath))
                        throw new FriendlyException($"Could not find PNG: {pngPath}");
                    ImageUtil.ReadPNG(pngPath, out Pixel[] image, out Color[] clut, out bool pngIndexed, out int pngBitDepth);
                    
                    byte mipmapTextures = pictureNode.ReadByte("mipmaptextures");
                    ushort width = pictureNode.ReadUInt16("width");
                    ushort height = pictureNode.ReadUInt16("height");
                    var imageType = Picture.GetColorTypeByName(pictureNode.ReadString("imagetype"));
                    bool indexed = imageType == Picture.ColorType.IndexColor4 || imageType == Picture.ColorType.IndexColor8;
                    int bitDepth = imageType == Picture.ColorType.IndexColor4 ? 4 : 8;
                    ImageUtil.ConvertPixelFormat(pngIndexed, indexed, pngBitDepth, bitDepth, false, image, clut, out image, out clut);

                    var picture = new Picture(width, height, (byte)(mipmapTextures - 1))
                    {
                        Image = image,
                        Clut = clut,
                        PictureFormat = pictureNode.ReadByte("pictformat"),
                        ImageColorType = imageType,
                        WriteExtendedHeader = pictureNode.ReadBooleanOrDefault("extendedheader", false),
                        Comment = pictureNode.ReadStringOrDefault("comment", string.Empty)
                    };

                    bool correctAlpha = pictureNode.ReadBooleanOrDefault("correctalpha", false);

                    if (indexed)
                    {
                        picture.ClutType = new Picture.ClutTypeConfig(
                            Picture.GetColorTypeByName(pictureNode.ReadString("cluttype/clutcolortype")),
                            pictureNode.ReadBoolean("cluttype/clutcompound"),
                            pictureNode.ReadEnum<Picture.ClutStorageModeType>("cluttype/csm"));
                    }

                    picture.GsTex.TextureBasePointer = pictureNode.ReadUInt16("gstex0/tbp0");
                    picture.GsTex.TextureBufferWidth = pictureNode.ReadByte("gstex0/tbw");
                    picture.GsTex.PixelStorageMode = pictureNode.ReadEnum<Picture.PixelStorageModeType>("gstex0/psm");
                    picture.GsTex.TextureWidth = pictureNode.ReadByte("gstex0/tw");
                    picture.GsTex.TextureHeight = pictureNode.ReadByte("gstex0/th");
                    picture.GsTex.TextureColorComponent = pictureNode.ReadEnum<Picture.TextureColorComponentType>("gstex0/tcc");
                    picture.GsTex.TextureFunction = pictureNode.ReadEnum<Picture.TextureFunctionType>("gstex0/tfx");
                    picture.GsTex.ClutBasePointer = pictureNode.ReadByte("gstex0/cbp");
                    picture.GsTex.ClutPixelStorageMode = pictureNode.ReadEnum<Picture.PixelStorageModeType>("gstex0/cpsm");
                    picture.GsTex.ClutStorageMode = pictureNode.ReadEnum<Picture.ClutStorageModeType>("gstex0/csm");
                    picture.GsTex.ClutStartAddress = pictureNode.ReadByte("gstex0/csa");
                    picture.GsTex.ClutLoadControl = pictureNode.ReadByte("gstex0/cld");

                    var gstex1Node = pictureNode.SelectSingleNode("gstex1");
                    if (gstex1Node != null)
                    {
                        picture.GsTex.LODCalculationMethod = gstex1Node.ReadByte("lcm");
                        picture.GsTex.MipLevelMax = gstex1Node.ReadByte("mxl");
                        picture.GsTex.MipMag = gstex1Node.ReadByte("mmag");
                        picture.GsTex.MipMin = gstex1Node.ReadByte("mmin");
                        picture.GsTex.MipmapTextureBaseAddress = gstex1Node.ReadByte("mtba");
                        picture.GsTex.LODParameterL = gstex1Node.ReadByte("l");
                        picture.GsTex.LODParameterK = gstex1Node.ReadUInt16("k");
                    }

                    var gstexafbapabeNode = pictureNode.SelectSingleNode("gstexafbapabe");
                    if (gstexafbapabeNode != null)
                    {
                        picture.GsTex.TA0 = gstexafbapabeNode.ReadByte("ta0");
                        picture.GsTex.AEM = gstexafbapabeNode.ReadByte("aem");
                        picture.GsTex.TA1 = gstexafbapabeNode.ReadByte("ta1");
                        picture.GsTex.PABE = gstexafbapabeNode.ReadByte("pabe");
                        picture.GsTex.FBA = gstexafbapabeNode.ReadByte("fba");
                    }

                    var gstexclutNode = pictureNode.SelectSingleNode("gstexclut");
                    if (gstexclutNode != null)
                    {
                        picture.GsTex.ClutBufferWidth = gstexclutNode.ReadByte("cbw");
                        picture.GsTex.ClutOffsetU = gstexclutNode.ReadByte("cou");
                        picture.GsTex.ClutOffsetV = gstexclutNode.ReadUInt16("cov");
                    }

                    if (mipmapTextures > 1)
                    {
                        var gsmiptbp1Node = pictureNode.SelectSingleNode("mipmaps/gsmiptbp1");
                        if (gsmiptbp1Node != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int level = i + 1;
                                picture.MipmapTextureBasePointers[i] = gsmiptbp1Node.ReadUInt16($"tbp{level}");
                                picture.MipmapTextureBufferWidths[i] = gsmiptbp1Node.ReadByte($"tbw{level}");
                            }
                        }

                        var gsmiptbp2Node = pictureNode.SelectSingleNode("mipmaps/gsmiptbp2");
                        if (gsmiptbp2Node != null)
                        {
                            for (int i = 3; i < 6; i++)
                            {
                                int level = i + 1;
                                picture.MipmapTextureBasePointers[i] = gsmiptbp2Node.ReadUInt16($"tbp{level}");
                                picture.MipmapTextureBufferWidths[i] = gsmiptbp2Node.ReadByte($"tbw{level}");
                            }
                        }

                        var filenameNodes = pictureNode.SelectNodes("mipmaps/filenames/filename");
                        if (filenameNodes != null)
                        {
                            foreach (XmlNode filenameNode in filenameNodes)
                            {
                                pngName = pictureNode.ReadString("filename");
                                pngPath = Path.Combine(folder, pngName);
                                if (!File.Exists(pngPath))
                                    throw new FriendlyException($"Could not find mipmap PNG: {pngPath}");
                                ImageUtil.ReadPNG(pngPath, out image, out clut, out pngIndexed, out pngBitDepth);
                                ImageUtil.ConvertPixelFormat(pngIndexed, indexed, pngBitDepth, bitDepth, true, image, clut, out image, out clut);
                                picture.Mipmaps.Add(image);
                            }
                        }
                    }

                    // Read subimages from userdata
                    var subImagesNode = pictureNode.SelectNodes("subimages/subimage");
                    if (subImagesNode != null)
                    {
                        picture.SubImages = new List<Picture.SubImage>(subImagesNode.Count);
                        foreach (XmlNode subImageNode in subImagesNode)
                        {
                            string subImageName = subImageNode.ReadString("name");
                            ushort subImageWidth = subImageNode.ReadUInt16("width");
                            ushort subImageHeight = subImageNode.ReadUInt16("height");
                            ushort subImageX = subImageNode.ReadUInt16("x");
                            ushort subImageY = subImageNode.ReadUInt16("y");
                            var subImage = new Picture.SubImage
                            {
                                Name = subImageName,
                                Width = subImageWidth,
                                Height = subImageHeight,
                                X = subImageX,
                                Y = subImageY
                            };

                            picture.SubImages.Add(subImage);
                        }
                    }

                    // Whether or not to set alpha to 0x80, 128, or 50%
                    // Sometimes images say they don't have alpha in GsTex0, but are using RGB32 (RGBA) storage, the alpha channel is set to 0x80 in this.
                    if (correctAlpha)
                    {
                        // Correct alpha on Clut
                        if (indexed)
                        {
                            for (int i = 0; i < picture.Clut.Length; i++)
                            {
                                picture.Clut[i] = Color.FromArgb(0x80, picture.Clut[i]);
                            }
                        }

                        // Correct alpha on LV0
                        for (int i = 0; i < picture.Image.Length; i++)
                        {
                            var pixel = picture.Image[i];
                            picture.Image[i] = new Pixel(Color.FromArgb(0x80, pixel.Color), pixel.Index);
                        }

                        // Correct alpha on mipmaps
                        for (int i = 0; i < picture.Mipmaps.Count; i++)
                        {
                            var mipmap = picture.Mipmaps[i];
                            for (int j = 0; j < mipmap.Length; j++)
                            {
                                var pixel = mipmap[j];
                                picture.Mipmaps[i][j] = new Pixel(Color.FromArgb(0x80, pixel.Color), pixel.Index);
                            }
                        }
                    }

                    file.Pictures.Add(picture);
                }
            }

            string outPath = Path.Combine(folder, outName);
            Util.Backup(outPath);
            file.Write(outPath);
        }
    }
}
