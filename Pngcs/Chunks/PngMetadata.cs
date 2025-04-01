using System;
using System.Collections.Generic;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Image metadata, a wrapper over a <see cref="ChunksList"/>.
    /// </summary>
    /// <remarks>
    /// Additional image info, apart from the ImageInfo and the pixels themselves.<br/>
    /// Includes Palette and ancillary chunks.<br/>
    /// This class provides a wrapper over the collection of chunks of a image (read or to write) and provides some high
    /// level methods to access them.
    /// </remarks>
    public class PngMetadata
    {
        /// <summary>
        /// The underlying <see cref="ChunksList"/>.
        /// </summary>
        private readonly ChunksList _chunkList;

        /// <summary>
        /// Basic image info, used for writing.
        /// </summary>
        private readonly ImageInfo _imageInfo;

        /// <summary>
        /// Whether or not the metadata is readonly.
        /// </summary>
        public readonly bool ReadOnly;

        internal PngMetadata(ChunksList chunks, ImageInfo imageInfo)
        {
            _chunkList = chunks;
            if (chunks is ChunksListForWrite)
            {
                ReadOnly = false;
            }
            else
            {
                ReadOnly = true;
            }

            _imageInfo = imageInfo;

        }

        #region Queue

        /// <summary>
        /// Queues the chunk at the writer.
        /// </summary>
        /// <param name="chunk">The ready-to-write chunk.</param>
        /// <param name="lazyOverwrite">Whether or not to overwrite lazily equivalent chunks.</param>
        /// <remarks>
        /// Warning: the overwriting applies to equivalent chunks, see <see cref="ChunkPredicateEquiv"/>,
        /// and will only make sense for queued (not yet writen) chunks.
        /// </remarks>
        public void QueueChunk(PngChunk chunk, bool lazyOverwrite)
        {
            ChunksListForWrite cl = (ChunksListForWrite)_chunkList;
            if (ReadOnly)
                throw new InvalidOperationException("Cannot queue chunk, metadata is readonly.");

            if (lazyOverwrite)
            {
                ChunkHelper.TrimList(cl.GetQueuedChunks(), new ChunkPredicateEquiv(chunk));
            }
            cl.Queue(chunk);
        }

        /// <summary>
        /// Queues the chunk at the writer.
        /// </summary>
        /// <param name="chunk">The ready-to-write chunk.</param>
        public void QueueChunk(PngChunk chunk)
            => QueueChunk(chunk, true);

        #endregion

        #region PHYS

        /// <summary>
        /// Returns physical resolution, in DPI, in both coordinates
        /// </summary>
        /// <returns>[dpix,dpiy], -1 if not set or unknown dimensions</returns>
        public double[] GetDpi()
        {
            PngChunk? c = _chunkList.GetById(ChunkHelper.pHYs, true);
            if (c == null)
                return [-1, -1];
            else
                return ((PngChunkPHYS)c).GetAsDpi2();
        }

        /// <summary>
        /// Sets physical resolution, in DPI.
        /// </summary>
        /// <remarks>This is a utility method that creates and enqueues a PHYS chunk.</remarks>
        /// <param name="dpix">Resolution in x.</param>
        /// <param name="dpiy">Resolution in y.</param>
        public void SetDpi(double dpix, double dpiy)
        {
            var c = new PngChunkPHYS(_imageInfo);
            c.SetAsDpi2(dpix, dpiy);
            QueueChunk(c);
        }

        /// <summary>
        /// Sets physical resolution, in DPI, both value in x and y dimensions
        /// </summary>
        /// <remarks>This is a utility method that creates and enqueues a PHYS chunk</remarks>
        /// <param name="dpi">Resolution in dpi</param>
        public void SetDpi(double dpi)
            => SetDpi(dpi, dpi);

        #endregion

        #region Time

        /// <summary>
        /// Creates a TIME chunk,  <c>nsecs</c> in the past from now.
        /// </summary>
        /// <param name="nsecs">Seconds in the past. If negative, it's a future time</param>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeNow(int nsecs)
        {
            var c = new PngChunkTIME(_imageInfo);
            c.SetNow(nsecs);
            QueueChunk(c);
            return c;
        }

        /// <summary>
        ///Creates a TIME chunk with current time.
        /// </summary>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeNow()
        {
            return SetTimeNow(0);
        }

        /// <summary>
        /// Creates a TIME chunk with given date and time
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="mon">Month (1-12)</param>
        /// <param name="day">Day of month (1-31)</param>
        /// <param name="hour">Hour (0-23)</param>
        /// <param name="min">Minute (0-59)</param>
        /// <param name="sec">Seconds (0-59)</param>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeYMDHMS(int year, int mon, int day, int hour, int min, int sec)
        {
            var c = new PngChunkTIME(_imageInfo);
            c.SetYMDHMS(year, mon, day, hour, min, sec);
            QueueChunk(c, true);
            return c;
        }

        /// <summary>
        /// Gets image timestamp, TIME chunk
        /// </summary>
        /// <returns>TIME chunk, null if not present</returns>
        public PngChunkTIME? GetTime()
        {
            var chunk = _chunkList.GetById(ChunkHelper.tIME);
            if (chunk == null)
                return null;

            return (PngChunkTIME)chunk;
        }

        /// <summary>
        /// Gets image timestamp, TIME chunk, as a String
        /// </summary>
        /// <returns>Formated TIME, empty string if not present</returns>
        public string GetTimeAsString()
        {
            PngChunkTIME? c = GetTime();
            return c == null ? string.Empty : c.GetAsString();
        }

        #endregion

        #region Text

        /// <summary>
        /// Creates a text chunk and enqueues it
        /// </summary>
        /// <param name="key">Key. Short and ASCII string</param>
        /// <param name="val">Text.</param>
        /// <param name="useLatin1">Flag. If false, will use UTF-8 (iTXt)</param>
        /// <param name="compress">Flag. Uses zTXt chunk.</param>
        /// <returns>The created and enqueued chunk</returns>
        public PngChunkTextVar SetText(string key, string val, bool useLatin1, bool compress)
        {
            if (compress && !useLatin1)
                throw new ArgumentException("Cannot compress non-latin text.");

            PngChunkTextVar c;
            if (useLatin1)
            {
                if (compress)
                {
                    c = new PngChunkZTXT(_imageInfo);
                }
                else
                {
                    c = new PngChunkTEXT(_imageInfo);
                }
            }
            else
            {
                c = new PngChunkITXT(_imageInfo);
                ((PngChunkITXT)c).LangTag = key; // we use the same orig tag (this is not quite right)
            }
            c.SetKeyValue(key, val);
            QueueChunk(c, true);
            return c;
        }

        /// <summary>
        /// Creates a plain text chunk (tEXT) and enqueues it
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Text</param>
        /// <returns>The created and enqueued chunk</returns>
        public PngChunkTextVar SetText(string key, string val)
        {
            return SetText(key, val, false, false);
        }

        /// <summary>
        /// Retrieves all text chunks with a given key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Empty list if nothing found</returns>
        /// <remarks>Can mix tEXt zTXt and iTXt chunks</remarks>
        public List<PngChunkTextVar> GetTxtsForKey(string key)
        {
            var li = new List<PngChunkTextVar>();
            foreach (PngChunk c in _chunkList.GetListById(ChunkHelper.tEXt, key))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in _chunkList.GetListById(ChunkHelper.zTXt, key))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in _chunkList.GetListById(ChunkHelper.iTXt, key))
                li.Add((PngChunkTextVar)c);
            return li;
        }

        /// <summary>
        /// Joins all strings for a given key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Concatenated (with newlines) if several found, empty string if none</returns>
        /// <remarks>You'd perhaps prefer GetTxtsForKey</remarks>
        public string GetTxtForKey(string key)
        {
            string t = "";
            List<PngChunkTextVar> li = GetTxtsForKey(key);
            if (li.Count == 0)
                return t;
            foreach (PngChunkTextVar c in li)
                t = t + c.GetValue() + "\n";
            return t.Trim();
        }

        #endregion

        #region PLTE

        public PngChunkPLTE? GetPLTE()
        {
            var chunk = _chunkList.GetById(PngChunkPLTE.ID);
            if (chunk == null)
                return null;

            return (PngChunkPLTE)chunk;
        }

        /// <summary>
        /// Creates a new empty PLTE chunk, queues it for write and return it to the caller, who should fill its entries.
        /// </summary>
        /// <returns>A new <see cref="PngChunkPLTE"/>.</returns>
        public PngChunkPLTE CreatePLTE()
        {
            var plte = new PngChunkPLTE(_imageInfo);
            QueueChunk(plte);
            return plte;
        }

        #endregion

        #region TRNS

        /// <summary>
        /// Returns the TRNS chunk or null if not present.
        /// </summary>
        /// <returns>A <see cref="PngChunkTRNS"/>.</returns>
        public PngChunkTRNS? GetTRNS()
        {
            var chunk = _chunkList.GetById(PngChunkTRNS.ID);
            if (chunk == null)
                return null;

            return (PngChunkTRNS)chunk;
        }

        /// <summary>
        /// Creates a new empty TRNS chunk, queues it for write and return it to the caller, who should fill its entries.
        /// </summary>
        /// <returns>A new <see cref="PngChunkTRNS"/>.</returns>
        public PngChunkTRNS CreateTRNS()
        {
            var trns = new PngChunkTRNS(_imageInfo);
            QueueChunk(trns);
            return trns;
        }

        #endregion
    }
}
