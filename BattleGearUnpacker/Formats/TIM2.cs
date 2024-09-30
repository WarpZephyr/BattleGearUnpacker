using BattleGearUnpacker.Core;
using BattleGearUnpacker.Core.Graphics;
using BattleGearUnpacker.Formats.Utility;
using BinaryMemory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace BattleGearUnpacker.Formats
{
    /// <summary>
    /// The PS2 TIM2 texture format.<br/>
    /// <br/>
    /// Extensions:<br/>
    /// .TM2<br/>
    /// .tm2
    /// </summary>
    public class TIM2 : FileFormat<TIM2>
    {
        #region Members

        /// <summary>
        /// The version of the format, only seen as 4.
        /// </summary>
        public byte FormatVersion { get; set; }

        /// <summary>
        /// The alignment of the format.
        /// </summary>
        public FormatAlignment FormatID { get; set; }

        /// <summary>
        /// The pictures in the file, usually only 1.
        /// </summary>
        public List<Picture> Pictures { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new <see cref="TIM2"/> with default settings.
        /// </summary>
        public TIM2()
        {
            FormatVersion = 4;
            FormatID = FormatAlignment.Align16;
            Pictures = [];
        }

        #endregion

        #region Is

        protected override bool Is(BinaryStreamReader br)
            => br.Length >= 16 && br.GetASCII(br.Position, 4) == "TIM2";

        #endregion

        #region Read

        /// <summary>
        /// Read a <see cref="TIM2"/> from a stream.
        /// </summary>
        /// <param name="br">The stream reader.</param>
        /// <exception cref="InvalidDataException">The FormatID was invalid.</exception>
        protected override void Read(BinaryStreamReader br)
        {
            br.BigEndian = false;
            br.AssertASCII("TIM2");
            FormatVersion = br.ReadByte();
            FormatID = (FormatAlignment)br.ReadByte();
            ushort pictureCount = br.ReadUInt16();

            // Check padding
            int alignment = FormatID == FormatAlignment.Align16 ? 16 :
                            FormatID == FormatAlignment.Align128 ? 128 :
                            throw new InvalidDataException($"Unknown {nameof(FormatID)}: {FormatID}");
            br.AssertBytePattern(alignment - 8, 0);

            // Read pictures
            Pictures = new List<Picture>(pictureCount);
            for (ushort i = 0; i < pictureCount; i++)
            {
                Pictures.Add(new Picture(br, alignment));
            }
        }

        #endregion

        #region Write

        /// <summary>
        /// Write a <see cref="TIM2"/> to a stream.
        /// </summary>
        /// <param name="bw">The stream writer.</param>
        /// <exception cref="InvalidDataException">The FormatID was invalid.</exception>
        protected override void Write(BinaryStreamWriter bw)
        {
            bw.BigEndian = false;
            bw.WriteASCII("TIM2");
            bw.WriteByte(FormatVersion);

            // Check padding
            int alignment = FormatID == FormatAlignment.Align16 ? 16 :
                            FormatID == FormatAlignment.Align128 ? 128 :
                            throw new InvalidDataException($"Unknown {nameof(FormatID)}: {FormatID}");
            bw.WriteByte((byte)FormatID);
            bw.WriteUInt16((ushort)Pictures.Count);
            bw.WriteBytes(new byte[alignment - 8]);

            // Write pictures
            foreach (var picture in Pictures)
            {
                picture.Write(bw, alignment);
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// The alignment major structures use.
        /// </summary>
        public enum FormatAlignment : byte
        {
            /// <summary>
            /// Align to 16 bytes.
            /// </summary>
            Align16 = 0,

            /// <summary>
            /// Align to 128 bytes.
            /// </summary>
            Align128 = 1
        }

        #endregion

        #region Structs

        /// <summary>
        /// A picture containing image data and settings.
        /// </summary>
        public class Picture
        {
            #region Members

            /// <summary>
            /// Picture format, only ever seen as 0.
            /// </summary>
            public byte PictureFormat { get; set; }

            /// <summary>
            /// Basic settings for the CLUT of the <see cref="Picture"/>.
            /// </summary>
            public ClutTypeConfig ClutType { get; set; }

            /// <summary>
            /// The color type of the image determining how image data is stored.
            /// </summary>
            public ColorType ImageColorType { get; set; }

            /// <summary>
            /// The width of the image.
            /// </summary>
            public ushort Width { get; set; }

            /// <summary>
            /// The height of the image.
            /// </summary>
            public ushort Height { get; set; }

            /// <summary>
            /// Configuration information for the image.
            /// </summary>
            public GsTexConfig GsTex { get; set; }

            /// <summary>
            /// Optional custom data a user of the format can use.
            /// </summary>
            public byte[] UserData { get; set; }

            /// <summary>
            /// A generic comment for the file.
            /// </summary>
            public string Comment { get; set; }

            /// <summary>
            /// The main image data, also called LV0.
            /// </summary>
            public Pixel[] Image { get; set; }

            /// <summary>
            /// Optional mipmaps of the image, with LV1-LV6 possible.
            /// </summary>
            public List<Pixel[]> Mipmaps { get; set; }

            /// <summary>
            /// Optional mipmap TextureBasePointer values stored in Gsmiptbp1 or Gsmiptbp2.<br/>
            /// Also called TBPn in Gsmiptbp1 or Gsmiptbp2, where n is the mipmap level.
            /// </summary>
            public ushort[] MipmapTextureBasePointers { get; private set; }

            /// <summary>
            /// Optional mipmap TextureBufferWidth values stored in Gsmiptbp1 or Gsmiptbp2.<br/>
            /// Also called TBWn in Gsmiptbp1 or Gsmiptbp2, where n is the mipmap level.
            /// </summary>
            public byte[] MipmapTextureBufferWidths { get; private set; }

            /// <summary>
            /// A color lookup table for indexed images.
            /// </summary>
            public Color[] Clut { get; set; }

            /// <summary>
            /// Whether or not to write an extended header when writing.<br/>
            /// Required to write <see cref="Comment"/>.
            /// </summary>
            public bool WriteExtendedHeader { get; set; }

            /// <summary>
            /// Whether or not the image is indexed.
            /// </summary>
            public bool Indexed => ImageColorType == ColorType.IndexColor4
                || ImageColorType == ColorType.IndexColor8;

            /// <summary>
            /// Whether or not the image has alpha.
            /// </summary>
            public bool HasAlpha =>
                GsTex.TextureColorComponent == TextureColorComponentType.RGBA
                && ((Indexed ? ClutType.ClutColorType : ImageColorType) != ColorType.RGB24);

            /// <summary>
            /// Gets the bit depth per pixel according to the color type.
            /// </summary>
            public int BitDepth
            {
                get
                {
                    return ImageColorType switch
                    {
                        ColorType.None => 0,
                        ColorType.IndexColor4 => 4,
                        ColorType.IndexColor8 => 8,
                        ColorType.RGB16 => 16,
                        ColorType.RGB24 => 24,
                        ColorType.RGB32 => 32,
                        _ => throw new InvalidOperationException($"Cannot get {nameof(BitDepth)} for {nameof(ColorType)}: {ImageColorType}"),
                    };
                }
            }

            #endregion

            #region Constructors

            /// <summary>
            /// Create a new <see cref="Picture"/> with default settings.
            /// </summary>
            /// <param name="width">The width of the image in pixels.</param>
            /// <param name="height">The height of the image in pixels.</param>
            public Picture(ushort width, ushort height)
            {
                PictureFormat = 0;
                ClutType = new ClutTypeConfig();
                ImageColorType = ColorType.RGB32;
                Width = width;
                Height = height;
                GsTex = new GsTexConfig();
                UserData = [];
                Comment = string.Empty;
                Image = new Pixel[width * height];
                Mipmaps = [];
                MipmapTextureBasePointers = new ushort[6];
                MipmapTextureBufferWidths = new byte[6];
                Clut = [];
                WriteExtendedHeader = false;
            }

            /// <summary>
            /// Create a new <see cref="Picture"/> with the specified settings.
            /// </summary>
            /// <param name="width">The width of the image in pixels.</param>
            /// <param name="height">The height of the image in pixels.</param>
            /// <param name="clutColorCount">The number of clut colors in the image.</param>
            /// <exception cref="ArgumentOutOfRangeException">A count argument was invalid.</exception>
            public Picture(ushort width, ushort height, ushort clutColorCount)
            {
                if (clutColorCount > 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(clutColorCount), $"The max a clut can support is {256} colors.");
                }

                PictureFormat = 0;
                ClutType = new ClutTypeConfig();
                ImageColorType = ColorType.RGB32;
                Width = width;
                Height = height;
                GsTex = new GsTexConfig();
                UserData = [];
                Comment = string.Empty;
                Image = new Pixel[width * height];
                Mipmaps = [];
                MipmapTextureBasePointers = new ushort[6];
                MipmapTextureBufferWidths = new byte[6];
                Clut = new Color[clutColorCount];
                WriteExtendedHeader = false;
            }

            /// <summary>
            /// Create a new <see cref="Picture"/> with the specified settings.
            /// </summary>
            /// <param name="width">The width of the image in pixels.</param>
            /// <param name="height">The height of the image in pixels.</param>
            /// <param name="mipMapCount">The number of mipmaps in the image.</param>
            /// <exception cref="ArgumentOutOfRangeException">A count argument was invalid.</exception>
            public Picture(ushort width, ushort height, byte mipMapCount)
            {
                if (mipMapCount > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(mipMapCount), $"There can only be {6} mipmaps other than LV0.");
                }

                PictureFormat = 0;
                ClutType = new ClutTypeConfig();
                ImageColorType = ColorType.RGB32;
                Width = width;
                Height = height;
                GsTex = new GsTexConfig();
                UserData = [];
                Comment = string.Empty;
                Image = new Pixel[width * height];
                Mipmaps = new List<Pixel[]>(mipMapCount);
                MipmapTextureBasePointers = new ushort[6];
                MipmapTextureBufferWidths = new byte[6];
                Clut = [];
                WriteExtendedHeader = false;
            }

            /// <summary>
            /// Create a new <see cref="Picture"/> with the specified settings.
            /// </summary>
            /// <param name="width">The width of the image in pixels.</param>
            /// <param name="height">The height of the image in pixels.</param>
            /// <param name="clutColorCount">The number of clut colors in the image.</param>
            /// <param name="mipMapCount">The number of mipmaps in the image.</param>
            /// <exception cref="ArgumentOutOfRangeException">A count argument was invalid.</exception>
            public Picture(ushort width, ushort height, ushort clutColorCount, byte mipMapCount)
            {
                if (clutColorCount > 256)
                {
                    throw new ArgumentOutOfRangeException(nameof(clutColorCount), $"The max a clut can support is {256} colors.");
                }

                if (mipMapCount > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(mipMapCount), $"There can only be {6} mipmaps other than LV0.");
                }

                PictureFormat = 0;
                ClutType = new ClutTypeConfig();
                ImageColorType = ColorType.RGB32;
                Width = width;
                Height = height;
                GsTex = new GsTexConfig();
                UserData = [];
                Comment = string.Empty;
                Image = new Pixel[width * height];
                Mipmaps = new List<Pixel[]>(mipMapCount);
                MipmapTextureBasePointers = new ushort[6];
                MipmapTextureBufferWidths = new byte[6];
                Clut = new Color[clutColorCount];
                WriteExtendedHeader = false;
            }

            /// <summary>
            /// Read a <see cref="Picture"/> from a stream.
            /// </summary>
            /// <param name="br">The stream reader.</param>
            /// <param name="alignment">The alignment of the format.</param>
            /// <exception cref="InvalidDataException">The data was detected to be invalid in some way.</exception>
            internal Picture(BinaryStreamReader br, int alignment)
            {
                br.ReadUInt32(); // TotalSize
                int clutSize = br.ReadInt32();
                int imageSize = br.ReadInt32();
                ushort headerSize = br.ReadUInt16();
                ushort clutColorCount = br.ReadUInt16();
                PictureFormat = br.ReadByte();
                byte mipMapCount = br.AssertByte([1, 2, 3, 4, 5, 6, 7]);
                ClutType = new ClutTypeConfig(br);
                ImageColorType = (ColorType)br.ReadByte();
                Width = br.ReadUInt16();
                Height = br.ReadUInt16();
                GsTex = new GsTexConfig(br);

                MipmapTextureBasePointers = new ushort[6];
                MipmapTextureBufferWidths = new byte[6];

                byte[] imageBytes;
                var mipmapBytes = new List<byte[]>(mipMapCount - 1);
                if (headerSize > 48)
                {
                    // Mipmap Header
                    int[] mipMapSizes;
                    int mipMapHeaderSize;
                    if (mipMapCount > 1)
                    {
                        long mipMapStart = br.Position;

                        // GsMiptbp1 bitfield
                        ulong gsMiptbp1 = br.ReadUInt64();
                        MipmapTextureBasePointers[0] = (ushort)(gsMiptbp1 & 0b0000_000000_00000000000000_000000_00000000000000_000000_11111111111111UL);
                        MipmapTextureBufferWidths[0] = (byte)((gsMiptbp1 & 0b0000_000000_00000000000000_000000_00000000000000_111111_00000000000000UL) >> 14);
                        MipmapTextureBasePointers[1] = (ushort)((gsMiptbp1 & 0b0000_000000_00000000000000_000000_11111111111111_000000_00000000000000UL) >> 20);
                        MipmapTextureBufferWidths[1] = (byte)((gsMiptbp1 & 0b0000_000000_00000000000000_111111_00000000000000_000000_00000000000000UL) >> 34);
                        MipmapTextureBasePointers[2] = (ushort)((gsMiptbp1 & 0b0000_000000_11111111111111_000000_00000000000000_000000_00000000000000UL) >> 40);
                        MipmapTextureBufferWidths[2] = (byte)((gsMiptbp1 & 0b0000_111111_00000000000000_000000_00000000000000_000000_00000000000000UL) >> 54);
                        byte gsMiptbp1Reserved = (byte)((gsMiptbp1 & 0b1111_000000_00000000000000_000000_00000000000000_000000_00000000000000UL) >> 60);
                        if (gsMiptbp1Reserved != 0)
                        {
                            throw new InvalidDataException("A reserved value is not 0, GsMiptbp1 is invalid.");
                        }

                        // GsMiptbp2 bitfield
                        ulong gsMiptbp2 = br.ReadUInt64();
                        MipmapTextureBasePointers[3] = (ushort)(gsMiptbp2 & 0b0000_000000_00000000000000_000000_00000000000000_000000_11111111111111UL);
                        MipmapTextureBufferWidths[3] = (byte)((gsMiptbp2 & 0b0000_000000_00000000000000_000000_00000000000000_111111_00000000000000UL) >> 14);
                        MipmapTextureBasePointers[4] = (ushort)((gsMiptbp2 & 0b0000_000000_00000000000000_000000_11111111111111_000000_00000000000000UL) >> 20);
                        MipmapTextureBufferWidths[4] = (byte)((gsMiptbp2 & 0b0000_000000_00000000000000_111111_00000000000000_000000_00000000000000UL) >> 34);
                        MipmapTextureBasePointers[5] = (ushort)((gsMiptbp2 & 0b0000_000000_11111111111111_000000_00000000000000_000000_00000000000000UL) >> 40);
                        MipmapTextureBufferWidths[5] = (byte)((gsMiptbp2 & 0b0000_111111_00000000000000_000000_00000000000000_000000_00000000000000UL) >> 54);
                        byte gsMiptbp2Reserved = (byte)((gsMiptbp2 & 0b1111_000000_00000000000000_000000_00000000000000_000000_00000000000000UL) >> 60);
                        if (gsMiptbp2Reserved != 0)
                        {
                            throw new InvalidDataException("A reserved value is not 0, GsMiptbp2 is invalid.");
                        }

                        mipMapSizes = br.ReadInt32s(mipMapCount);
                        br.AssertBytePattern((int)(MathHelper.BinaryAlign(br.Position, 16L) - br.Position), 0);
                        mipMapHeaderSize = (int)(br.Position - mipMapStart);
                    }
                    else
                    {
                        mipMapHeaderSize = 0;
                        mipMapSizes = [];
                    }

                    // Extended Header
                    int userDataSize;
                    int commentSize;
                    if (br.GetASCII(br.Position, 4) == "eXt")
                    {
                        WriteExtendedHeader = true;
                        br.Position += 4;
                        int userSpaceSize = br.ReadInt32();
                        userDataSize = br.ReadInt32();
                        br.AssertUInt32(0);
                        commentSize = userSpaceSize - 16 - userDataSize;
                    }
                    else
                    {
                        WriteExtendedHeader = false;
                        userDataSize = headerSize - 48 - mipMapHeaderSize;
                        commentSize = 0;
                    }

                    // User Data
                    if (userDataSize > 0)
                        UserData = br.ReadBytes(userDataSize);
                    else
                        UserData = [];

                    // Comment
                    if (commentSize > 0)
                        // Comment is null terminated, but just in case read all the required bytes safely.
                        Comment = br.ReadASCII(commentSize);
                    else
                        Comment = string.Empty;

                    // Image Data
                    // Image is aligned to format alignment.
                    br.AssertBytePattern((int)(MathHelper.BinaryAlign(br.Position, alignment) - br.Position), 0);
                    if (mipMapCount > 1)
                    {
                        // Separate mipmaps and LV0.
                        imageBytes = br.ReadBytes(mipMapSizes[0]);
                        for (int i = 1; i < mipMapCount; i++)
                        {
                            // All mipmaps after LV0 are aligned to 16.
                            br.AssertBytePattern((int)(MathHelper.BinaryAlign(br.Position, 16) - br.Position), 0);
                            mipmapBytes.Add(br.ReadBytes(mipMapSizes[i]));
                        }
                    }
                    else
                    {
                        // No mipmaps, only LV0.
                        imageBytes = br.ReadBytes(imageSize);
                    }
                }
                else
                {
                    WriteExtendedHeader = false;
                    UserData = [];
                    Comment = string.Empty;

                    // Image Data
                    // Image is aligned to format alignment.
                    br.AssertBytePattern((int)(MathHelper.BinaryAlign(br.Position, alignment) - br.Position), 0);
                    imageBytes = br.ReadBytes(imageSize);
                }

                // CLUT Data
                if (clutSize > 0 && clutColorCount > 0)
                {
                    // CLUT is aligned to format alignment.
                    br.AssertBytePattern((int)(MathHelper.BinaryAlign(br.Position, alignment) - br.Position), 0);
                    Clut = ReadCLUT(br, clutSize, clutColorCount);
                }
                else
                {
                    // No CLUT.
                    Clut = [];
                }

                Image = ReadPixels(Width, Height, ImageColorType, imageBytes, Clut);

                // Convert mipmaps to pixels
                Mipmaps = new List<Pixel[]>(mipMapCount - 1);
                for (int i = 0; i < mipmapBytes.Count; i++)
                {
                    Mipmaps.Add(ReadPixels((ushort)(Width >>> (i + 1)), (ushort)(Height >>> (i + 1)), ImageColorType, mipmapBytes[i], Clut));
                }
            }

            #endregion

            #region Read

            /// <summary>
            /// Read the pixels of image data.
            /// </summary>
            /// <param name="width">The width of the passed image data.</param>
            /// <param name="height">The height of the passed image data.</param>
            /// <param name="colorType">The color type of the image determining how it is stored.</param>
            /// <param name="image">The raw image data to read.</param>
            /// <param name="clut">The processed CLUT of the image data if neccessary.</param>
            /// <returns>The read pixels.</returns>
            /// <exception cref="InvalidOperationException">The color type of the image data was found to be invalid.</exception>
            private static Pixel[] ReadPixels(ushort width, ushort height, ColorType colorType, byte[] image, Color[] clut)
            {
                int pixelIndex = 0;
                Pixel[] pixels = new Pixel[width * height];

                for (int i = 0; i < image.Length;)
                {
                    switch (colorType)
                    {
                        case ColorType.RGB16:
                            int rawValue = image[i + 1] << 8 | image[i];
                            pixels[pixelIndex++] = new Pixel(ColorDecode.FromA1B5G5R5(rawValue), -1);
                            i += 2;
                            break;
                        case ColorType.RGB24:
                            pixels[pixelIndex++] = new Pixel(Color.FromArgb(image[i], image[i + 1], image[i + 2]), -1);
                            i += 3;
                            break;
                        case ColorType.RGB32:
                            pixels[pixelIndex++] = new Pixel(Color.FromArgb(image[i + 3], image[i], image[i + 1], image[i + 2]), -1);
                            i += 4;
                            break;
                        case ColorType.IndexColor4:
                            // Do two at once
                            byte indexRawValue = image[i];
                            int index1 = indexRawValue >> 4;
                            int index2 = indexRawValue & 0b00001111;
                            pixels[pixelIndex++] = new Pixel(clut[index1], index1);
                            pixels[pixelIndex++] = new Pixel(clut[index2], index2);
                            i++;
                            break;
                        case ColorType.IndexColor8:
                            int index = image[i];
                            pixels[pixelIndex++] = new Pixel(clut[index], index);
                            i++;
                            break;
                        case ColorType.None:
                        default:
                            throw new InvalidOperationException($"Invalid {nameof(ColorType)} for {nameof(ReadPixels)}: {colorType}");
                    }
                }

                return pixels;
            }

            /// <summary>
            /// Read the CLUT data from a stream, rearranging to make it sequential if necessary.
            /// </summary>
            /// <param name="br">The stream reader.</param>
            /// <param name="clutSize">The size of the CLUT in bytes.</param>
            /// <param name="colorCount">The number of colors in the CLUT.</param>
            /// <returns>An array of colors representing the CLUT.</returns>
            /// <exception cref="InvalidOperationException">The color type of the CLUT was found to be invalid.</exception>
            private Color[] ReadCLUT(BinaryStreamReader br, int clutSize, int colorCount)
            {
                byte[] clutBytes = br.ReadBytes(clutSize);
                var colors = new Color[colorCount];
                int byteIndex = 0;

                // Read every color into a managed structure.
                for (int i = 0; i < colorCount; i++)
                {
                    switch (ClutType.ClutColorType)
                    {
                        case ColorType.RGB16:
                            int rawValue = clutBytes[byteIndex++] | clutBytes[byteIndex++] << 8;
                            colors[i] = ColorDecode.FromA1B5G5R5(rawValue);
                            break;
                        case ColorType.RGB24:
                            colors[i] = Color.FromArgb(clutBytes[byteIndex++], clutBytes[byteIndex++], clutBytes[byteIndex++]);
                            break;
                        case ColorType.RGB32:
                            colors[i] = Color.FromArgb(clutBytes[byteIndex + 3], clutBytes[byteIndex], clutBytes[byteIndex + 1], clutBytes[byteIndex + 2]);
                            byteIndex += 4;
                            break;
                        case ColorType.IndexColor4:
                        case ColorType.IndexColor8:
                        case ColorType.None:
                        default:
                            throw new InvalidOperationException($"Invalid {nameof(ColorType)} for {nameof(ReadCLUT)}: {ClutType.ClutColorType}");
                    }
                }

                // Swap the two middle sets of every 32 colors if compounded.
                // CSM2 is never compounded as far as I know.
                // CSM1 can be sequential if the ClutCompound flag is false for 4-bit indexed images, this is due to possibly only 16 colors being present there.
                if (ClutType.ClutStorageMode == ClutStorageModeType.CSM1 && !(ImageColorType == ColorType.IndexColor4 && ClutType.ClutCompound == false))
                {
                    CompoundColors(colors);
                }

                return colors;
            }

            #endregion

            #region Write

            /// <summary>
            /// Write this <see cref="Picture"/> to a stream.
            /// </summary>
            /// <param name="bw">The stream writer.</param>
            /// <param name="alignment">The format alignment specified by the header.</param>
            internal void Write(BinaryStreamWriter bw, int alignment)
            {
                // Header
                long pictureStart = bw.Position;
                bw.ReserveUInt32("TotalSize");
                bw.ReserveUInt32("ClutSize");
                bw.ReserveUInt32("ImageSize");
                bw.ReserveUInt16("HeaderSize");
                bw.WriteUInt16((ushort)Clut.Length);
                bw.WriteByte(PictureFormat);

                int mipmapCount = Mipmaps.Count > 6 ? 7 : Mipmaps.Count < 1 ? 1 : Mipmaps.Count;
                bw.WriteByte((byte)mipmapCount);
                ClutType.Write(bw);
                bw.WriteByte((byte)ImageColorType);
                bw.WriteUInt16(Width);
                bw.WriteUInt16(Height);
                GsTex.Write(bw);

                // Write Mipmap Header
                if (Mipmaps.Count > 0)
                {
                    // Pack GsMiptbp1
                    int bitPosition = 0;
                    uint gsMiptbp1 = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        gsMiptbp1 = BitPacker.LeftPackUInt32(gsMiptbp1, MipmapTextureBasePointers[i], bitPosition);
                        bitPosition += 14;

                        gsMiptbp1 = BitPacker.LeftPackUInt32(gsMiptbp1, MipmapTextureBufferWidths[i], bitPosition);
                        bitPosition += 6;
                    }
                    gsMiptbp1 = BitPacker.LeftPackUInt32(gsMiptbp1, 0U, 60);
                    bw.WriteUInt32(gsMiptbp1);

                    // Pack GsMiptbp2
                    bitPosition = 0;
                    uint gsMiptbp2 = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        gsMiptbp2 = BitPacker.LeftPackUInt32(gsMiptbp2, MipmapTextureBasePointers[i + 3], bitPosition);
                        bitPosition += 14;

                        gsMiptbp2 = BitPacker.LeftPackUInt32(gsMiptbp2, MipmapTextureBufferWidths[i + 3], bitPosition);
                        bitPosition += 6;
                    }
                    gsMiptbp2 = BitPacker.LeftPackUInt32(gsMiptbp2, 0U, 60);
                    bw.WriteUInt32(gsMiptbp2);

                    // Reserve Sizes
                    for (int i = 0; i < mipmapCount; i++)
                        bw.ReserveUInt32($"MIPMAP_LV{i}_SIZE");

                    // Pad to 16
                    bw.Pad(16);
                }

                // Write Extended Header
                if (WriteExtendedHeader)
                {
                    bw.WriteASCII("eXt\0", false);
                    bw.WriteUInt32((uint)(16 + UserData.Length + Comment.Length + 1)); // UserSpaceSize
                    bw.WriteUInt32((uint)UserData.Length); // UserDataSize
                    bw.WriteUInt32(0U); // Reserved
                }

                // Write User Data
                if (UserData.Length > 0)
                {
                    bw.WriteBytes(UserData);
                }

                // Write Comment
                if (Comment.Length > 0 && WriteExtendedHeader)
                {
                    bw.WriteASCII(Comment, true);
                }

                // Header End
                bw.Pad(alignment); // Pad to format alignment
                long headerEnd = bw.Position;
                bw.FillUInt16("HeaderSize", (ushort)(headerEnd - pictureStart));

                // Write Image Data
                long imageStart = bw.Position;
                WritePixels(bw);
                bw.Pad(alignment); // Pad to format alignment
                long imageEnd = bw.Position;
                bw.FillUInt32("ImageSize", (uint)(imageEnd - imageStart));

                // Write Clut Data
                long pictureEnd;
                if (ImageColorType == ColorType.IndexColor4 || ImageColorType == ColorType.IndexColor8)
                {
                    WriteCLUT(bw, Clut);
                    pictureEnd = bw.Position;
                    bw.FillUInt32("ClutSize", (uint)(pictureEnd - imageEnd));
                }
                else
                {
                    pictureEnd = bw.Position;
                    bw.FillUInt32("ClutSize", 0U);
                }

                bw.FillUInt32("TotalSize", (uint)(pictureEnd - pictureStart));
            }

            /// <summary>
            /// Write pixel data to a stream.
            /// </summary>
            /// <param name="bw">The stream writer.</param>
            /// <param name="clut">The clut of the pixels.</param>
            /// <exception cref="NotSupportedException">The true color method tried to write indexed which isn't supported there.</exception>
            /// <exception cref="ArgumentException">The color type for the image was invalid for writing pixels.</exception>
            private void WritePixels(BinaryStreamWriter bw)
            {
                static void WriteTrueColor(BinaryStreamWriter bw, Pixel[] pixels, ushort width, ushort height, ColorType colorType)
                {
                    int pixelCount = width * height;
                    for (int i = 0; i < pixelCount; i++)
                    {
                        // Write bytes
                        switch (colorType)
                        {
                            case ColorType.RGB16:
                                bw.WriteUInt16(ColorDecode.ToA1B5G5R5(pixels[i].Color));
                                break;
                            case ColorType.RGB24:
                                bw.WriteColorRGB(pixels[i].Color);
                                break;
                            case ColorType.RGB32:
                                bw.WriteColorRGBA(pixels[i].Color);
                                break;
                            case ColorType.IndexColor4:
                            case ColorType.IndexColor8:
                                throw new NotSupportedException($"{nameof(ColorType.IndexColor4)} and {nameof(ColorType.IndexColor8)} are not supported in {nameof(WriteTrueColor)}");
                            case ColorType.None:
                            default:
                                throw new ArgumentException($"Invalid {nameof(ColorType)} for {nameof(WritePixels)}: {colorType}.", nameof(colorType));
                        }
                    }
                }

                static void WriteIndexed(BinaryStreamWriter bw, Pixel[] pixels, ushort width, ushort height, ColorType colorType)
                {
                    if (colorType == ColorType.IndexColor4)
                    {
                        bool even = (pixels.Length % 2) == 0;
                        int length = even ? pixels.Length : pixels.Length - 1;
                        for (int i = 0; i < length; i++)
                        {
                            bw.WriteByte((byte)(pixels[i++].Index << 4 | pixels[i].Index));
                        }

                        if (!even)
                            bw.WriteByte((byte)(pixels[length].Index << 4));
                    }
                    else if (colorType == ColorType.IndexColor8)
                    {
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            bw.WriteByte((byte)pixels[i].Index);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException($"{nameof(ColorType)} {colorType} is not supported in {nameof(WriteIndexed)}");
                    }
                }

                // Indexed image
                if (ImageColorType == ColorType.IndexColor4 || ImageColorType == ColorType.IndexColor8)
                {
                    // Write image
                    long start = bw.Position;
                    WriteIndexed(bw, Image, Width, Height, ImageColorType);
                    bw.Pad(16);
                    long end = bw.Position;

                    if (Mipmaps.Count > 0)
                    {
                        bw.FillUInt32("MIPMAP_LV0_SIZE", (uint)(end - start));

                        // Write mipmaps
                        for (int i = 0; i < Mipmaps.Count; i++)
                        {
                            start = bw.Position;
                            int level = i + 1;
                            ushort mipmapWidth = (ushort)(Width >>> level);
                            ushort mipmapHeight = (ushort)(Height >>> level);
                            WriteIndexed(bw, Mipmaps[i], mipmapWidth, mipmapHeight, ImageColorType);
                            bw.Pad(16);
                            end = bw.Position;
                            bw.FillUInt32($"MIPMAP_LV{level}_SIZE", (uint)(end - start));
                        }
                    }

                    return;
                }
                else // TrueColor image
                {
                    // Write image
                    long start = bw.Position;
                    WriteTrueColor(bw, Image, Width, Height, ImageColorType);
                    bw.Pad(16);
                    long end = bw.Position;

                    if (Mipmaps.Count > 0)
                    {
                        bw.FillUInt32("MIPMAP_LV0_SIZE", (uint)(end - start));

                        // Write mipmaps
                        for (int i = 0; i < Mipmaps.Count; i++)
                        {
                            start = bw.Position;
                            int level = i + 1;
                            WriteTrueColor(bw, Mipmaps[i], (ushort)(Width >>> level), (ushort)(Height >>> level), ImageColorType);
                            bw.Pad(16);
                            end = bw.Position;
                            bw.FillUInt32($"MIPMAP_LV{level}_SIZE", (uint)(end - start));
                        }
                    }
                }
            }

            /// <summary>
            /// Write clut data to a stream.
            /// </summary>
            /// <param name="bw">The stream writer.</param>
            /// <param name="clut">The clut to write.</param>
            /// <exception cref="InvalidOperationException">The color type for the image or clut was invalid for writing a clut.</exception>
            private void WriteCLUT(BinaryStreamWriter bw, Color[] clut)
            {
                int colorCount = ImageColorType == ColorType.IndexColor8 ? 256 :
                    ImageColorType == ColorType.IndexColor4 ? (ClutType.ClutCompound ? 32 : 16) :
                    throw new InvalidOperationException($"Invalid {nameof(ImageColorType)} for {nameof(WriteCLUT)}: {ImageColorType}");

                // Get final color array of proper size to write
                Color[] colors = new Color[colorCount];
                Array.Copy(clut, colors, colorCount > clut.Length ? clut.Length : colorCount);

                // Swap the two middle sets of every 32 colors if compounded.
                // CSM2 is never compounded as far as I know.
                // CSM1 can be sequential if the ClutCompound flag is false for 4-bit indexed images, this is due to possibly only 16 colors being present there.
                if (ClutType.ClutStorageMode == ClutStorageModeType.CSM1 && !(ImageColorType == ColorType.IndexColor4 && ClutType.ClutCompound == false))
                {
                    CompoundColors(colors);
                }

                // Check if GsTex requires alpha to be ignored.
                bool includeAlpha = GsTex.TextureColorComponent == TextureColorComponentType.RGBA;
                for (int i = 0; i < colorCount; i++)
                {
                    switch (ClutType.ClutColorType)
                    {
                        case ColorType.RGB16:
                            bw.WriteUInt16(ColorDecode.ToA1B5G5R5(colors[i]));
                            break;
                        case ColorType.RGB24:
                            bw.WriteColorRGB(colors[i]);
                            break;
                        case ColorType.RGB32:
                            bw.WriteColorRGBA(colors[i]);
                            break;
                        case ColorType.IndexColor4:
                        case ColorType.IndexColor8:
                        case ColorType.None: // Writer should not get to this function if there is no Clut and the user set things correctly.
                        default:
                            throw new InvalidOperationException($"Invalid {nameof(ClutType.ClutColorType)} for {nameof(WriteCLUT)}: {ClutType.ClutColorType}");
                    }
                }
            }

            #endregion

            #region Helpers

            /// <summary>
            /// Compounds a color array for a Clut buffer.
            /// </summary>
            /// <param name="colors">The colors to compound.</param>
            private static void CompoundColors(Color[] colors)
            {
                // Set 1, Set 3
                // Set 2, Set 4
                // Swap sets 3 and 2.
                // TODO: Investigate any better way to do this.
                Color[] buffer = new Color[8];
                for (int i = 0; i < colors.Length;)
                {
                    // Step into the middle
                    i += 8;

                    // Get the index of the second set to swap
                    int second = i + 8;

                    // Store set 3 colors in buffer
                    buffer[0] = colors[i];
                    buffer[1] = colors[i + 1];
                    buffer[2] = colors[i + 2];
                    buffer[3] = colors[i + 3];
                    buffer[4] = colors[i + 4];
                    buffer[5] = colors[i + 5];
                    buffer[6] = colors[i + 6];
                    buffer[7] = colors[i + 7];

                    // Swap set 3 to set 2 position
                    colors[i] = colors[second];
                    colors[i + 1] = colors[second + 1];
                    colors[i + 2] = colors[second + 2];
                    colors[i + 3] = colors[second + 3];
                    colors[i + 4] = colors[second + 4];
                    colors[i + 5] = colors[second + 5];
                    colors[i + 6] = colors[second + 6];
                    colors[i + 7] = colors[second + 7];

                    // Swap set 2 to set 3 from buffer
                    colors[second] = buffer[0];
                    colors[second + 1] = buffer[1];
                    colors[second + 2] = buffer[2];
                    colors[second + 3] = buffer[3];
                    colors[second + 4] = buffer[4];
                    colors[second + 5] = buffer[5];
                    colors[second + 6] = buffer[6];
                    colors[second + 7] = buffer[7];

                    // Jump to the next sets to process
                    i += 24;
                }
            }

            #endregion

            #region Methods

            /// <summary>
            /// Get the name of a <see cref="ColorType"/>.
            /// </summary>
            /// <param name="colorType">The <see cref="ColorType"/> to get the name of.</param>
            /// <returns>The name of the specified <see cref="ColorType"/>.</returns>
            /// <exception cref="InvalidOperationException">The specified <see cref="ColorType"/> was invalid.</exception>
            public static string GetColorTypeName(ColorType colorType)
            {
                return colorType switch
                {
                    ColorType.None => "TIM2_NONE",
                    ColorType.RGB16 => "TIM2_RGB16",
                    ColorType.RGB24 => "TIM2_RGB24",
                    ColorType.RGB32 => "TIM2_RGB32",
                    ColorType.IndexColor4 => "TIM2_IDTEX4",
                    ColorType.IndexColor8 => "TIM2_IDTEX8",
                    _ => throw new InvalidOperationException($"Invalid {nameof(ColorType)}: {colorType}"),
                };
            }

            /// <summary>
            /// Get a <see cref="ColorType"/> by name.
            /// </summary>
            /// <param name="name">The name of the <see cref="ColorType"/> to get.</param>
            /// <returns>The <see cref="ColorType"/> of the specified name.</returns>
            /// <exception cref="NotSupportedException">The specified name was unknown.</exception>
            public static ColorType GetColorTypeByName(string name)
            {
                return name switch
                {
                    "TIM2_NONE" => ColorType.None,
                    "TIM2_RGB16" => ColorType.RGB16,
                    "TIM2_RGB24" => ColorType.RGB24,
                    "TIM2_RGB32" => ColorType.RGB32,
                    "TIM2_IDTEX4" => ColorType.IndexColor4,
                    "TIM2_IDTEX8" => ColorType.IndexColor8,
                    _ => throw new NotSupportedException($"Unknown {nameof(ColorType)} name: {name}"),
                };
            }

            #endregion

            #region Enums

            /// <summary>
            /// The color type determining how the color is stored and how many bits are used.
            /// </summary>
            public enum ColorType : byte
            {
                /// <summary>
                /// The data contains nothing, only valid for CLUT.
                /// </summary>
                None = 0,

                /// <summary>
                /// The data is RGBA packed into 16-bits or 2 bytes, organized as A1B5G5R5.
                /// </summary>
                RGB16 = 1,

                /// <summary>
                /// The data is RGB, organized as R8G8B8.
                /// </summary>
                RGB24 = 2,

                /// <summary>
                /// The data is RGBA, organized as R8G8B8A8.
                /// </summary>
                RGB32 = 3,

                /// <summary>
                /// The data uses 4-bit indexes for a CLUT meaning 2 for every byte, only valid for image data.
                /// </summary>
                IndexColor4 = 4,

                /// <summary>
                /// The data uses 8-bit indexes for a CLUT meaning 1 per byte, only valid for image data.
                /// </summary>
                IndexColor8 = 5
            }

            /// <summary>
            /// The storage mode of the CLUT determining the order of CLUT colors.
            /// </summary>
            public enum ClutStorageModeType : byte
            {
                /// <summary>
                /// The CLUT data is stored compounded, with colors 16-23 and 8-15 in each 32 colors being swapped.<br/>
                /// If the image color type is 4-bit indexed, and the ClutCompound flag is off,<br/>
                /// it's possible the CLUT can only have 16 colors, meaning the colors are not swapped.
                /// </summary>
                CSM1 = 0,

                /// <summary>
                /// The CLUT data is stored sequentially.
                /// </summary>
                CSM2 = 1
            }

            /// <summary>
            /// The storage mode of the pixels of the image in local memory.
            /// </summary>
            public enum PixelStorageModeType : byte
            {
                /// <summary>
                /// RGBA32, uses 32-bits per pixel.
                /// </summary>
                PSMCT32 = 0,

                /// <summary>
                /// RGB24, uses 24-bits per pixel.
                /// </summary>
                PSMCT24 = 1,

                /// <summary>
                /// RGBA16 unsigned, pack two pixels in 32-bits in little endian order.
                /// </summary>
                PSMCT16 = 2,

                /// <summary>
                /// RGBA16 signed, pack two pixels in 32-bits in little endian order.
                /// </summary>
                PSMCT16S = 10,

                /// <summary>
                /// 8-bit indexed, packing 4 pixels per 32-bits.
                /// </summary>
                PSMT8 = 19,

                /// <summary>
                /// 4-bit indexed, packing 8 pixels per 32-bits.
                /// </summary>
                PSMT4 = 20,

                /// <summary>
                /// 8-bit indexed, but the upper 24-bits are unused.
                /// </summary>
                PSMT8H = 27,

                /// <summary>
                /// 4-bit indexed, but the upper 24-bits are unused.
                /// </summary>
                PSMT4HL = 36,

                /// <summary>
                /// 4-bit indexed, where the bits 4-7 are evaluated and the rest are discarded.
                /// </summary>
                PSMT4HH = 44,

                /// <summary>
                /// 32-bit Z buffer.
                /// </summary>
                PSMZ32 = 48,

                /// <summary>
                /// 24-bit Z buffer with the upper 8-bits unused.
                /// </summary>
                PSMZ24 = 49,

                /// <summary>
                /// 16-bit unsigned Z buffer, pack two pixels in 32-bits in little endian order.
                /// </summary>
                PSMZ16 = 50,

                /// <summary>
                /// 16-bit signed Z buffer, pack two pixels in 32-bits in little endian order.
                /// </summary>
                PSMZ16S = 58
            }

            /// <summary>
            /// The texture color component flag determining if alpha is included.
            /// </summary>
            public enum TextureColorComponentType : byte
            {
                /// <summary>
                /// Alpha is to be ignored.
                /// </summary>
                RGB = 0,

                /// <summary>
                /// Alpha is included.
                /// </summary>
                RGBA = 1
            }

            /// <summary>
            /// The texture function type, purpose unclear.
            /// </summary>
            public enum TextureFunctionType : byte
            {
                /// <summary>
                /// Modulate.
                /// </summary>
                Modulate = 0,

                /// <summary>
                /// Decal.
                /// </summary>
                Decal = 1,

                /// <summary>
                /// Hilight.
                /// </summary>
                Hilight = 2,

                /// <summary>
                /// Hilight 2.
                /// </summary>
                Hilight2 = 3
            }

            #endregion

            #region Structs

            /// <summary>
            /// CLUT data configuration.
            /// </summary>
            public class ClutTypeConfig
            {
                #region Members

                /// <summary>
                /// The color type of the CLUT.
                /// </summary>
                public ColorType ClutColorType { get; set; }

                /// <summary>
                /// The CLUT compound flag, meaning a 4-bit indexed image has at least 32 colors in it's CLUT to allow compounding.
                /// </summary>
                public bool ClutCompound { get; set; }

                /// <summary>
                /// The storage mode of the CLUT, determining if it is sequential or compounded.
                /// </summary>
                public ClutStorageModeType ClutStorageMode { get; set; }

                /// <summary>
                /// Gets the bit depth per color according to the color type.
                /// </summary>
                public int BitDepth
                {
                    get
                    {
                        return ClutColorType switch
                        {
                            ColorType.None => 0,
                            ColorType.IndexColor4 => 4,
                            ColorType.IndexColor8 => 8,
                            ColorType.RGB16 => 16,
                            ColorType.RGB24 => 24,
                            ColorType.RGB32 => 32,
                            _ => throw new InvalidOperationException($"Cannot get {nameof(BitDepth)} for {nameof(ColorType)}: {ClutColorType}"),
                        };
                    }
                }

                #endregion

                #region Constructors

                /// <summary>
                /// Create a new <see cref="ClutTypeConfig"/> with default settings.
                /// </summary>
                public ClutTypeConfig()
                {
                    ClutColorType = ColorType.None;
                    ClutCompound = false;
                    ClutStorageMode = ClutStorageModeType.CSM1;
                }

                /// <summary>
                /// Create a new <see cref="ClutTypeConfig"/> with the specified settings.
                /// </summary>
                /// <param name="clutColorType">The <see cref="ColorType"/> of the clut.</param>
                /// <param name="clutCompound">Whether or not the clut is to be compounded.</param>
                /// <param name="clutStorageMode">The storage mode of the clut.</param>
                public ClutTypeConfig(ColorType clutColorType, bool clutCompound, ClutStorageModeType clutStorageMode)
                {
                    ClutColorType = clutColorType;
                    ClutCompound = clutCompound;
                    ClutStorageMode = clutStorageMode;
                }

                /// <summary>
                /// Read a <see cref="ClutTypeConfig"/> from a stream.
                /// </summary>
                /// <param name="br">The stream reader.</param>
                internal ClutTypeConfig(BinaryStreamReader br)
                {
                    byte clutType = br.ReadByte();
                    ClutColorType = (ColorType)BitPacker.LeftUnpackByte(clutType, 6, 0);
                    ClutCompound = BitPacker.LeftUnpackByte(clutType, 1, 6) == 1;
                    ClutStorageMode = (ClutStorageModeType)BitPacker.LeftUnpackByte(clutType, 1, 7);
                }

                #endregion

                #region Write

                /// <summary>
                /// Writes this <see cref="ClutTypeConfig"/> to a stream.
                /// </summary>
                /// <param name="bw">The stream writer.</param>
                /// <exception cref="InvalidOperationException">The data to write was invalid.</exception>
                internal void Write(BinaryStreamWriter bw)
                {
                    if (ClutStorageMode > ClutStorageModeType.CSM2)
                        throw new InvalidOperationException($"Invalid {nameof(ClutStorageMode)}: {ClutStorageMode}.");
                    if (ClutColorType > ColorType.RGB32)
                        throw new InvalidOperationException($"Invalid {nameof(ClutColorType)}: {ClutColorType}.");

                    // Pack ClutType
                    byte clutType = 0;
                    clutType = BitPacker.LeftPackByte(clutType, (byte)ClutColorType, 0);
                    clutType = BitPacker.LeftPackByte(clutType, (byte)(ClutCompound ? 1 : 0), 6);
                    clutType = BitPacker.LeftPackByte(clutType, (byte)ClutStorageMode, 7);
                    bw.WriteByte(clutType);
                }

                #endregion
            }

            /// <summary>
            /// Configuration of the image containing data from GsTex0, GsTex1, GsTexaFbaPabe, and GsTexClut.
            /// </summary>
            public class GsTexConfig
            {
                #region Members

                /// <summary>
                /// The texture base pointer (Address/256).<br/>
                /// Also called TBP0 in GsTex0.
                /// </summary>
                public ushort TextureBasePointer { get; set; }

                /// <summary>
                /// The texture buffer width (Texels/64).<br/>
                /// Also called TBW in GsTex0.
                /// </summary>
                public byte TextureBufferWidth { get; set; }

                /// <summary>
                /// The texture pixel storage format.<br/>
                /// Also called PSM in GsTex0.
                /// </summary>
                public PixelStorageModeType PixelStorageMode { get; set; }

                /// <summary>
                /// A log2 of the texture width.<br/>
                /// Also called TW in GsTex0.
                /// </summary>
                public byte TextureWidth { get; set; }

                /// <summary>
                /// A log2 of the texture height.<br/>
                /// Also called TH in GsTex0.
                /// </summary>
                public byte TextureHeight { get; set; }

                /// <summary>
                /// Determines whether or not alpha is included.<br/>
                /// Also called TCC in GsTex0.
                /// </summary>
                public TextureColorComponentType TextureColorComponent { get; set; }

                /// <summary>
                /// Texture function, purpose unclear.<br/>
                /// Also called TFX in GsTex0.
                /// </summary>
                public TextureFunctionType TextureFunction { get; set; }

                /// <summary>
                /// The Clut buffer location.<br/>
                /// Multiply it by 0x100 to get the raw VRAM pointer.<br/>
                /// Also called CBP in GsTex0.
                /// </summary>
                public ushort ClutBasePointer { get; set; }

                /// <summary>
                /// The pixel storage format of the CLUT.<br/>
                /// Also called CPSM in GsTex0.
                /// </summary>
                public PixelStorageModeType ClutPixelStorageMode { get; set; }

                /// <summary>
                /// The storage mode of the CLUT which determines the order of the colors.<br/>
                /// Also called CSM in GsTex0.
                /// </summary>
                public ClutStorageModeType ClutStorageMode { get; set; }

                /// <summary>
                /// The Clut Entry Offset.<br/>
                /// Mostly used by 4-bit images.<br/>
                /// Also called CSA in GsTex0.
                /// </summary>
                public byte ClutStartAddress { get; set; }

                /// <summary>
                /// Clut Load control, purpose unknown.<br/>
                /// Also called CLD in GsTex0.
                /// </summary>
                public byte ClutLoadControl { get; set; }

                /// <summary>
                /// The LOD calculation method, purpose unknown.<br/>
                /// Also called LCM in GsTex1.
                /// </summary>
                public byte LODCalculationMethod { get; set; }

                /// <summary>
                /// The maximum mipmap level<br/>
                /// Set to 0 when no mipmaps exist, and only LV0 does.<br/>
                /// Also called MXL in GsTex1.
                /// </summary>
                public byte MipLevelMax { get; set; }

                /// <summary>
                /// The filter for when texture is expanded.<br/>
                /// Also called MMAG in GsTex1.
                /// </summary>
                public byte MipMag { get; set; }

                /// <summary>
                /// The filter for when texture is reduced.<br>
                /// Also called MMIN in GsTex1.
                /// </summary>
                public byte MipMin { get; set; }

                /// <summary>
                /// The location of mipmaps LV1 and greater.<br/>
                /// Also called MTBA in GsTex1.
                /// </summary>
                public byte MipmapTextureBaseAddress { get; set; }

                /// <summary>
                /// The L parameter value of LOD, purpose unknown.<br/>
                /// Also called L in GsTex1.
                /// </summary>
                public byte LODParameterL { get; set; }

                /// <summary>
                /// The K parameter value of LOD, purpose unknown.<br/>
                /// Also called K in GsTex1.
                /// </summary>
                public ushort LODParameterK { get; set; }

                /// <summary>
                /// The TA0 field of the TEXA register, purpose unknown.<br/>
                /// Also called TA0 in GsTexaFbaPabe.
                /// </summary>
                public byte TA0 { get; set; }

                /// <summary>
                /// The AEM bit of the TEXA register, purpose unknown.<br/>
                /// Also called AEM in GsTexaFbaPabe.
                /// </summary>
                public byte AEM { get; set; }

                /// <summary>
                /// The TA1 field of the TEXA register, purpose unknown.<br/>
                /// Also called TA1 in GsTexaFbaPabe.
                /// </summary>
                public byte TA1 { get; set; }

                /// <summary>
                /// The PABE bit of the PABE register, purpose unknown.<br/>
                /// Also called PABE in GsTexaFbaPabe.
                /// </summary>
                public byte PABE { get; set; }

                /// <summary>
                /// The FBA bit of the FBA_1 and FBA_2 registers, purpose unknown.<br/>
                /// Also called FBA in GsTexaFbaPabe.
                /// </summary>
                public byte FBA { get; set; }

                /// <summary>
                /// The CLUT buffer width in the TEXCLUT register, purpose unknown.<br/>
                /// Also called CBW in GsTexClut.
                /// </summary>
                public byte ClutBufferWidth { get; set; }

                /// <summary>
                /// The CLUT offset U value in the TEXCLUT register, purpose unknown.<br/>
                /// Also called COU in GsTexClut.
                /// </summary>
                public byte ClutOffsetU { get; set; }

                /// <summary>
                /// The CLUT offset V value in the TEXCLUT register, purpose unknown.<br/>
                /// Also called COV in GsTexClut.
                /// </summary>
                public ushort ClutOffsetV { get; set; }

                #endregion

                #region Constructors

                /// <summary>
                /// Create a new <see cref="GsTexConfig"/> with default settings.
                /// </summary>
                public GsTexConfig()
                {
                    TextureBasePointer = 0;
                    TextureBufferWidth = 0;
                    PixelStorageMode = PixelStorageModeType.PSMCT32;
                    TextureWidth = 0;
                    TextureHeight = 0;
                    TextureColorComponent = TextureColorComponentType.RGB;
                    TextureFunction = TextureFunctionType.Modulate;
                    ClutBasePointer = 0;
                    ClutPixelStorageMode = PixelStorageModeType.PSMCT32;
                    ClutStartAddress = 0;
                    ClutLoadControl = 0;
                    LODCalculationMethod = 0;
                    MipLevelMax = 0;
                    MipMag = 1;
                    MipMin = 1;
                    MipmapTextureBaseAddress = 1;
                    LODParameterL = 0;
                    LODParameterK = 0;
                    TA0 = 0;
                    AEM = 0;
                    TA1 = 0;
                    PABE = 0;
                    FBA = 0;
                    ClutBufferWidth = 0;
                    ClutOffsetU = 0;
                    ClutOffsetV = 0;
                }

                /// <summary>
                /// Read a <see cref="GsTexConfig"/> from a stream.
                /// </summary>
                /// <param name="br">The stream reader.</param>
                /// <exception cref="InvalidDataException">The data was detected to be invalid in some way.</exception>
                internal GsTexConfig(BinaryStreamReader br)
                {
                    // Read the GsTex0 bitfield values.
                    ulong gsTex0 = br.ReadUInt64();
                    TextureBasePointer = (ushort)(gsTex0 & 0b000_00000_0_0000_00000000000000_00_0_0000_0000_000000_000000_11111111111111UL);
                    TextureBufferWidth = (byte)((gsTex0 & 0b000_00000_0_0000_00000000000000_00_0_0000_0000_000000_111111_00000000000000UL) >> 14);
                    PixelStorageMode = (PixelStorageModeType)((gsTex0 & 0b000_00000_0_0000_00000000000000_00_0_0000_0000_111111_000000_00000000000000UL) >> 20);
                    TextureWidth = (byte)((gsTex0 & 0b000_00000_0_0000_00000000000000_00_0_0000_1111_000000_000000_00000000000000UL) >> 26);
                    TextureHeight = (byte)((gsTex0 & 0b000_00000_0_0000_00000000000000_00_0_1111_0000_000000_000000_00000000000000UL) >> 30);
                    TextureColorComponent = (TextureColorComponentType)((gsTex0 & 0b000_00000_0_0000_00000000000000_00_1_0000_0000_000000_000000_00000000000000UL) >> 34);
                    TextureFunction = (TextureFunctionType)((gsTex0 & 0b000_00000_0_0000_00000000000000_11_0_0000_0000_000000_000000_00000000000000UL) >> 35);
                    ClutBasePointer = (ushort)((gsTex0 & 0b000_00000_0_0000_11111111111111_00_0_0000_0000_000000_000000_00000000000000UL) >> 37);
                    ClutPixelStorageMode = (PixelStorageModeType)((gsTex0 & 0b000_00000_0_1111_00000000000000_00_0_0000_0000_000000_000000_00000000000000UL) >> 51);
                    ClutStorageMode = (ClutStorageModeType)((gsTex0 & 0b000_00000_1_0000_00000000000000_00_0_0000_0000_000000_000000_00000000000000UL) >> 55);
                    ClutStartAddress = (byte)((gsTex0 & 0b000_11111_0_0000_00000000000000_00_0_0000_0000_000000_000000_00000000000000UL) >> 56);
                    ClutLoadControl = (byte)((gsTex0 & 0b111_00000_0_0000_00000000000000_00_0_0000_0000_000000_000000_00000000000000UL) >> 61);

                    // Read the GsTex1 bitfield values.
                    ulong gsTex1 = br.ReadUInt64();
                    LODCalculationMethod = (byte)(gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_0_000_0_000_0_1UL);
                    byte gsTex1Reserved0 = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_0_000_0_000_1_0UL) >> 1);
                    MipLevelMax = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_0_000_0_111_0_0UL) >> 2);
                    MipMag = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_0_000_1_000_0_0UL) >> 5);
                    MipMin = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_0_111_0_000_0_0UL) >> 6);
                    MipmapTextureBaseAddress = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_000000000_1_000_0_000_0_0UL) >> 9);
                    ushort gsTex1Reserved1 = (ushort)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_00_111111111_0_000_0_000_0_0UL) >> 10);
                    LODParameterL = (byte)((gsTex1 & 0b00000000000000000000_000000000000_00000000000_11_000000000_0_000_0_000_0_0UL) >> 19);
                    ushort gsTex1Reserved2 = (ushort)((gsTex1 & 0b00000000000000000000_000000000000_11111111111_00_000000000_0_000_0_000_0_0UL) >> 21);
                    LODParameterK = (ushort)((gsTex1 & 0b00000000000000000000_111111111111_00000000000_00_000000000_0_000_0_000_0_0UL) >> 32);
                    ushort gsTex1Reserved3 = (ushort)((gsTex1 & 0b11111111111111111111_000000000000_00000000000_00_000000000_0_000_0_000_0_0UL) >> 44);
                    if (gsTex1Reserved0 != 0
                     || gsTex1Reserved1 != 0
                     || gsTex1Reserved2 != 0
                     || gsTex1Reserved3 != 0)
                    {
                        throw new InvalidDataException("A reserved value is not 0, GsTex1 is invalid.");
                    }

                    // Read the GsTexaFbaPabe bitfield values.
                    uint gsTexaFbaPabe = br.ReadUInt32();
                    TA0 = (byte)(gsTexaFbaPabe & 0b0_0_000000_00000000_0_0000000_11111111U);
                    byte gsTexaFbaPabeReserved0 = (byte)((gsTexaFbaPabe & 0b0_0_000000_00000000_0_1111111_00000000U) >> 8);
                    AEM = (byte)((gsTexaFbaPabe & 0b0_0_000000_00000000_1_0000000_00000000U) >> 15);
                    TA1 = (byte)((gsTexaFbaPabe & 0b0_0_000000_11111111_0_0000000_00000000U) >> 16);
                    byte gsTexaFbaPabeReserved1 = (byte)((gsTexaFbaPabe & 0b0_0_111111_00000000_0_0000000_00000000U) >> 24);
                    PABE = (byte)((gsTexaFbaPabe & 0b0_1_000000_00000000_0_0000000_00000000U) >> 30);
                    FBA = (byte)((gsTexaFbaPabe & 0b1_0_000000_00000000_0_0000000_00000000U) >> 31);
                    if (gsTexaFbaPabeReserved0 != 0
                     || gsTexaFbaPabeReserved1 != 0)
                    {
                        throw new InvalidDataException("A reserved value is not 0, GsTexaFbaPabe is invalid.");
                    }

                    // Read the GsTexClut bitfield values.
                    uint gsTexClut = br.ReadUInt32();
                    ClutBufferWidth = (byte)(gsTexClut & 0b0000000000_0000000000_000000_111111U);
                    ClutOffsetU = (byte)((gsTexClut & 0b0000000000_0000000000_111111_000000U) >> 6);
                    ClutOffsetV = (ushort)((gsTexClut & 0b0000000000_1111111111_000000_000000U) >> 12);
                    ushort gsTexClutReserved0 = (ushort)((gsTexClut & 0b1111111111_0000000000_000000_000000U) >> 22);
                    if (gsTexClutReserved0 != 0)
                    {
                        throw new InvalidDataException("A reserved value is not 0, GsTexClut is invalid.");
                    }
                }

                #endregion

                #region Write

                /// <summary>
                /// Write this <see cref="GsTexConfig"/> to a stream.
                /// </summary>
                /// <param name="bw">The stream writer.</param>
                internal void Write(BinaryStreamWriter bw)
                {
                    // Pack GsTex0
                    ulong gsTex0 = 0;
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, TextureBasePointer, 0);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, TextureBufferWidth, 14);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, (byte)PixelStorageMode, 20);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, TextureWidth, 26);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, TextureHeight, 30);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, (byte)TextureColorComponent, 34);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, (byte)TextureFunction, 35);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, ClutBasePointer, 37);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, (byte)ClutPixelStorageMode, 51);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, (byte)ClutStorageMode, 55);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, ClutStartAddress, 56);
                    gsTex0 = BitPacker.LeftPackUInt64(gsTex0, ClutLoadControl, 61);
                    bw.WriteUInt64(gsTex0);

                    // Pack GsTex1
                    ulong gsTex1 = 0;
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, LODCalculationMethod, 0);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, 0UL, 1);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, MipLevelMax, 2);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, MipMag, 5);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, MipMin, 6);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, MipmapTextureBaseAddress, 9);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, 0UL, 10);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, LODParameterL, 19);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, 0UL, 21);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, LODParameterK, 32);
                    gsTex1 = BitPacker.LeftPackUInt64(gsTex1, 0UL, 44);
                    bw.WriteUInt64(gsTex1);

                    // Pack GsTexaFbaPabe
                    uint gsTexaFbaPabe = 0;
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, TA0, 0);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, 0U, 8);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, AEM, 15);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, TA1, 16);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, 0, 24);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, PABE, 30);
                    gsTexaFbaPabe = BitPacker.LeftPackUInt32(gsTexaFbaPabe, FBA, 31);
                    bw.WriteUInt32(gsTexaFbaPabe);

                    // Pack GsTexClut
                    uint gsTexClut = 0;
                    gsTexClut = BitPacker.LeftPackUInt32(gsTexClut, ClutBufferWidth, 0);
                    gsTexClut = BitPacker.LeftPackUInt32(gsTexClut, ClutOffsetU, 6);
                    gsTexClut = BitPacker.LeftPackUInt32(gsTexClut, ClutOffsetV, 12);
                    gsTexClut = BitPacker.LeftPackUInt32(gsTexClut, 0U, 22);
                    bw.WriteUInt32(gsTexClut);
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}
