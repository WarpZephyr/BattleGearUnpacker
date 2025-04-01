using System;
using System.Collections.Generic;
using System.IO;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Chunks written or queued to be written 
    /// http://www.w3.org/TR/PNG/#table53
    /// </summary>
    ///
    public class ChunksListForWrite : ChunksList
    {
        /// <summary>
        /// Unwritten chunks, does not include IHDR, IDAT, END, perhaps yes PLTE.
        /// </summary>
        private readonly List<PngChunk> _queuedChunks;

        // redundant, just for efficiency
        private readonly Dictionary<string, int> alreadyWrittenKeys;

        internal ChunksListForWrite() : base()
        {
            _queuedChunks = [];
            alreadyWrittenKeys = [];
        }

        /// <summary>
        /// Same as <c>getById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<PngChunk> GetQueuedById(string id)
            => GetQueuedById(id, null);

        /// <summary>
        /// Same as <c>GetById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="innerid"></param>
        /// <returns></returns>
        public List<PngChunk> GetQueuedById(string id, string? innerid)
            => GetXById(_queuedChunks, id, innerid);

        /// <summary>
        /// Same as <c>GetById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="innerid"></param>
        /// <param name="failIfMultiple"></param>
        /// <returns></returns>
        public PngChunk? GetQueuedById1(string id, string? innerid, bool failIfMultiple)
        {
            List<PngChunk> list = GetQueuedById(id, innerid);
            if (list.Count == 0)
                return null;
            if (list.Count > 1 && (failIfMultiple || !list[0].AllowsMultiple()))
                throw new Exception($"Unexpected duplicate chunk IDs: {id}");

            return list[^1];
        }

        /// <summary>
        /// Same as <c>GetById1()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="failIfMultiple"></param>
        /// <returns></returns>
        public PngChunk? GetQueuedById1(string id, bool failIfMultiple)
            => GetQueuedById1(id, null, failIfMultiple);

        /// <summary>
        /// Same as getById1(), but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PngChunk? GetQueuedById1(string id)
            => GetQueuedById1(id, false);

        /// <summary>
        ///Remove Chunk: only from queued 
        /// </summary>
        /// <remarks>
        /// WARNING: this depends on chunk.Equals() implementation, which is straightforward for SingleChunks. For 
        /// MultipleChunks, it will normally check for reference equality!
        /// </remarks>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool RemoveChunk(PngChunk c)
            => _queuedChunks.Remove(c);

        /// <summary>
        /// Adds a chunk to the queue.
        /// </summary>
        /// <remarks>Does not check for duplicated chunks.</remarks>
        /// <param name="chunk">The chunk to queue.</param>
        /// <returns></returns>
        public bool Queue(PngChunk chunk)
        {
            _queuedChunks.Add(chunk);
            return true;
        }

        /// <summary>
        /// This should be called only for ancillary chunks and PLTE (groups 1 - 3 - 5).
        /// </summary>a
        private static bool ShouldWrite(PngChunk c, int currentGroup)
        {
            if (currentGroup == CHUNK_GROUP_2_PLTE)
                return c.Id.Equals(ChunkHelper.PLTE);
            if (currentGroup % 2 == 0)
                throw new InvalidDataException("Bad chunk group?");

            int minChunkGroup, maxChunkGroup;
            if (c.MustGoBeforePLTE())
            {
                minChunkGroup = maxChunkGroup = CHUNK_GROUP_1_AFTERIDHR;
            }
            else if (c.MustGoBeforeIDAT())
            {
                maxChunkGroup = CHUNK_GROUP_3_AFTERPLTE;
                minChunkGroup = c.MustGoAfterPLTE() ? CHUNK_GROUP_3_AFTERPLTE : CHUNK_GROUP_1_AFTERIDHR;
            }
            else
            {
                maxChunkGroup = CHUNK_GROUP_5_AFTERIDAT;
                minChunkGroup = CHUNK_GROUP_1_AFTERIDHR;
            }

            int preferred = maxChunkGroup;
            if (c.Priority)
                preferred = minChunkGroup;
            if (ChunkHelper.IsUnknown(c) && c.ChunkGroup > 0)
                preferred = c.ChunkGroup;
            if (currentGroup == preferred)
                return true;
            if (currentGroup > preferred && currentGroup <= maxChunkGroup)
                return true;

            return false;
        }

        internal int WriteChunks(Stream output, int currentGroup)
        {
            var written = new List<int>();
            for (int i = 0; i < _queuedChunks.Count; i++)
            {
                PngChunk c = _queuedChunks[i];
                if (!ShouldWrite(c, currentGroup))
                    continue;
                if (ChunkHelper.IsCritical(c.Id) && !c.Id.Equals(ChunkHelper.PLTE))
                    throw new InvalidOperationException($"Bad chunk queued: {c}");
                if (alreadyWrittenKeys.ContainsKey(c.Id) && !c.AllowsMultiple())
                    throw new InvalidOperationException($"Duplicates of this chunk are not allowed: {c}");

                c.Write(output);
                _chunks.Add(c);
                alreadyWrittenKeys[c.Id] = alreadyWrittenKeys.TryGetValue(c.Id, out int value) ? value + 1 : 1;
                written.Add(i);
                c.ChunkGroup = currentGroup;
            }
            for (int k = written.Count - 1; k >= 0; k--)
            {
                _queuedChunks.RemoveAt(written[k]);
            }
            return written.Count;
        }

        /// <summary>
        /// Unwritten chunks, does not include IHDR, IDAT, END, perhaps yes PLTE.
        /// </summary>
        /// <returns>This is not a copy, don't modify.</returns>
        internal List<PngChunk> GetQueuedChunks()
            => _queuedChunks;
    }
}
