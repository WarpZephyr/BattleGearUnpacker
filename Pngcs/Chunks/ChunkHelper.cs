using Pngcs.Helpers;
using Pngcs.IO.Zlib;
using System.Collections.Generic;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Static utility methods for Chunks.
    /// </summary>
    /// <remarks>
    /// Client code should rarely need this, see PngMetada and ChunksList.
    /// </remarks>
    public static class ChunkHelper
    {
        internal const string IHDR = "IHDR";
        internal const string PLTE = "PLTE";
        internal const string IDAT = "IDAT";
        internal const string IEND = "IEND";
        internal const string cHRM = "cHRM";// No Before PLTE and IDAT
        internal const string gAMA = "gAMA";// No Before PLTE and IDAT
        internal const string iCCP = "iCCP";// No Before PLTE and IDAT
        internal const string sBIT = "sBIT";// No Before PLTE and IDAT
        internal const string sRGB = "sRGB";// No Before PLTE and IDAT
        internal const string bKGD = "bKGD";// No After PLTE; before IDAT
        internal const string hIST = "hIST";// No After PLTE; before IDAT
        internal const string tRNS = "tRNS";// No After PLTE; before IDAT
        internal const string pHYs = "pHYs";// No Before IDAT
        internal const string sPLT = "sPLT";// Yes Before IDAT
        internal const string tIME = "tIME";// No None
        internal const string iTXt = "iTXt";// Yes None
        internal const string tEXt = "tEXt";// Yes None
        internal const string zTXt = "zTXt";// Yes None
        internal static readonly byte[] IHDR_BYTES = ToBytes(IHDR);
        internal static readonly byte[] PLTE_BYTES = ToBytes(PLTE);
        internal static readonly byte[] IDAT_BYTES = ToBytes(IDAT);
        internal static readonly byte[] IEND_BYTES = ToBytes(IEND);

        /// <summary>
        /// Converts to bytes using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ToBytes(string bytes)
            => PngHelperInternal.CharsetLatin1.GetBytes(bytes);

        /// <summary>
        /// Converts to String using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToString(byte[] bytes)
            => PngHelperInternal.CharsetLatin1.GetString(bytes);

        /// <summary>
        ///  Converts to String using Latin1 (ISO-8859-1)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string ToString(byte[] bytes, int offset, int len)
            => PngHelperInternal.CharsetLatin1.GetString(bytes, offset, len);

        /// <summary>
        /// Converts to bytes using UTF-8
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ToBytesUTF8(string bytes)
            => PngHelperInternal.CharsetUTF8.GetBytes(bytes);

        /// <summary>
        /// Converts to string using UTF-8
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToStringUTF8(byte[] bytes)
            => PngHelperInternal.CharsetUTF8.GetString(bytes);

        /// <summary>
        /// Converts to string using UTF-8
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToStringUTF8(byte[] bytes, int offset, int length)
            => PngHelperInternal.CharsetUTF8.GetString(bytes, offset, length);

        /// <summary>
        /// Simplifies writing an array of bytes to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="bytes">The bytes to write.</param>
        public static void WriteBytes(Stream stream, byte[] bytes)
            => stream.Write(bytes, 0, bytes.Length);

        /// <summary>
        /// Whether or not a chunk is critical, specified by the first letter being uppercase.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        public static bool IsCritical(string id)
            => char.IsUpper(id[0]);

        /// <summary>
        /// Whether or not a chunk is public, specified by the second letter being uppercase.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        public static bool IsPublic(string id)
            => char.IsUpper(id[1]);

        /// <summary>
        /// Whether or not a chunk is safe to copy, specified by the fourth letter being lowercase.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        public static bool IsSafeToCopy(string id)
           => char.IsLower(id[3]);

        /// <summary>
        /// We consider a chunk as "unknown" if our chunk factory (even when it has been augmented by client code) doesn't recognize it
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public static bool IsUnknown(PngChunk chunk)
            => chunk is PngChunkUNKNOWN;

        public static bool IsText(PngChunk c)
            => c is PngChunkTextVar;

        /// <summary>
        /// Finds position of null byte in array
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>-1 if not found</returns>
        public static int PosNullByte(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] == 0)
                    return i;
            return -1;
        }

        /// <summary>
        /// Decides if a chunk should be loaded, according to a ChunkLoadBehavior.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="behavior"></param>
        /// <returns></returns>
        public static bool ShouldLoad(string id, ChunkLoadBehavior behavior)
        {
            if (IsCritical(id))
                return true;
            bool known = PngChunk.IsKnown(id);
            switch (behavior)
            {
                case ChunkLoadBehavior.LOAD_CHUNK_ALWAYS:
                    return true;
                case ChunkLoadBehavior.LOAD_CHUNK_IF_SAFE:
                    return known || IsSafeToCopy(id);
                case ChunkLoadBehavior.LOAD_CHUNK_KNOWN:
                    return known;
                case ChunkLoadBehavior.LOAD_CHUNK_NEVER:
                    return false;
            }
            return false; // should not reach here
        }

        internal static byte[] CompressBytes(byte[] original, bool compress)
            => CompressBytes(original, 0, original.Length, compress);

        internal static byte[] CompressBytes(byte[] original, int offset, int length, bool compress)
        {
            Stream input = new MemoryStream(original, offset, length);
            if (!compress)
                input = ZlibStreamFactory.CreateZlibInputStream(input);

            Stream output = new MemoryStream();
            if (compress)
                output = ZlibStreamFactory.CreateZlibOutputStream(output);

            ShovelInputToOutput(input, output);
            byte[] result = ((MemoryStream)output).ToArray();

            input.Dispose();
            output.Dispose();
            return result;
        }

        private static void ShovelInputToOutput(Stream input, Stream output)
        {
            byte[] buffer = new byte[1024];

            int length;
            while ((length = input.Read(buffer, 0, 1024)) > 0)
            {
                output.Write(buffer, 0, length);
            }
        }

        internal static bool MaskMatch(int v, int mask)
            => (v & mask) != 0;

        /// <summary>
        /// Filters a list of Chunks, keeping those which match the predicate
        /// </summary>
        /// <remarks>The original list is not altered</remarks>
        /// <param name="list"></param>
        /// <param name="predicateKeep"></param>
        /// <returns></returns>
        public static List<PngChunk> FilterList(List<PngChunk> list, IChunkPredicate predicateKeep)
        {
            List<PngChunk> result = new List<PngChunk>();
            foreach (PngChunk element in list)
            {
                if (predicateKeep.Matches(element))
                {
                    result.Add(element);
                }
            }
            return result;
        }

        /// <summary>
        /// Filters a list of Chunks, removing those which match the predicate.
        /// </summary>
        /// <remarks>The original list is not altered.</remarks>
        public static int TrimList(List<PngChunk> list, IChunkPredicate predicateRemove)
        {
            int cont = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicateRemove.Matches(list[i]))
                {
                    list.RemoveAt(i);
                    cont++;
                }
            }
            return cont;
        }

        /// <summary>
        /// Ad-hoc criteria for 'equivalent' chunks.
        /// </summary>
        ///  <remarks>
        /// Two chunks are equivalent if they have the same Id AND either:
        /// 1. they are Single
        /// 2. both are textual and have the same key
        /// 3. both are SPLT and have the same palette name
        /// Bear in mind that this is an ad-hoc, non-standard, nor required (nor wrong)
        /// criterion. Use it only if you find it useful. Notice that PNG allows to have
        /// repeated textual keys with same keys.
        /// </remarks>        
        /// <param name="c1">Chunk1</param>
        /// <param name="c2">Chunk1</param>
        /// <returns>true if equivalent</returns>
        public static bool Equivalent(PngChunk c1, PngChunk c2)
        {
            if (c1 == c2)
                return true;

            if (c1 == null || c2 == null || !c1.Id.Equals(c2.Id))
                return false;

            // Same id
            if (c1.GetType() != c2.GetType())
                return false; // Should not happen

            if (!c2.AllowsMultiple())
                return true;

            if (c1 is PngChunkTextVar textVar)
            {
                return textVar.GetKey().Equals(((PngChunkTextVar)c2).GetKey());
            }
            else if (c1 is PngChunkSPLT sPLT)
            {
                return sPLT.PalName.Equals(((PngChunkSPLT)c2).PalName);
            }

            // Unknown chunks that allow multiple?
            // Assume they don't match.
            return false;
        }
    }
}
