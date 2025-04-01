using Pngcs.Helpers;
using System;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// tIME chunk: http://www.w3.org/TR/PNG/#11tIME
    /// </summary>
    public class PngChunkTIME : PngChunkSingle
    {
        public const string ID = ChunkHelper.tIME;

        private int year;
        private int month;
        private int day;
        private int hour;
        private int minute;
        private int second;

        public PngChunkTIME(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint()
            => ChunkOrderingConstraint.NONE;

        public override ChunkRaw CreateRawChunk()
        {
            ChunkRaw c = CreateEmptyChunk(7, true);
            PngHelperInternal.WriteInt16(year, c.Data, 0);
            c.Data[2] = (byte)month;
            c.Data[3] = (byte)day;
            c.Data[4] = (byte)hour;
            c.Data[5] = (byte)minute;
            c.Data[6] = (byte)second;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk)
        {
            if (chunk.Length != 7)
                throw new InvalidDataException($"Invalid chunk length: {chunk}");

            year = PngHelperInternal.ReadInt16(chunk.Data, 0);
            month = PngHelperInternal.ReadByte(chunk.Data, 2);
            day = PngHelperInternal.ReadByte(chunk.Data, 3);
            hour = PngHelperInternal.ReadByte(chunk.Data, 4);
            minute = PngHelperInternal.ReadByte(chunk.Data, 5);
            second = PngHelperInternal.ReadByte(chunk.Data, 6);
        }

        public override void CloneDataFromRead(PngChunk other)
        {
            PngChunkTIME x = (PngChunkTIME)other;
            year = x.year;
            month = x.month;
            day = x.day;
            hour = x.hour;
            minute = x.minute;
            second = x.second;
        }

        public void SetNow(int secsAgo)
        {
            DateTime d1 = DateTime.Now;
            year = d1.Year;
            month = d1.Month;
            day = d1.Day;
            hour = d1.Hour;
            minute = d1.Minute;
            second = d1.Second;
        }

        internal void SetYMDHMS(int yearx, int monx, int dayx, int hourx, int minx, int secx)
        {
            year = yearx;
            month = monx;
            day = dayx;
            hour = hourx;
            minute = minx;
            second = secx;
        }

        public int[] GetYMDHMS()
            => new int[] { year, month, day, hour, minute, second };

        /// <summary>
        /// format YYYY/MM/DD HH:mm:SS
        /// </summary>
        public string GetAsString()
            => string.Format("%04d/%02d/%02d %02d:%02d:%02d", year, month, day, hour, minute, second);

    }
}
