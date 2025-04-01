using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Pngcs.Chunks
{
    /// <summary>
    /// Represents a instance of a PNG chunk
    /// </summary>
    /// <remarks>
    /// Concrete classes should extend <c>PngChunkSingle</c> or <c>PngChunkMultiple</c>
    /// 
    /// Note that some methods/fields are type-specific (GetOrderingConstraint(), AllowsMultiple())
    /// some are 'almost' type-specific (Id,Crit,Pub,Safe; the exception is <c>PngUKNOWN</c>), 
    /// and some are instance-specific
    /// 
    /// Ref: http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
    /// </remarks>
    public abstract class PngChunk
    {
        /// <summary>
        /// Restrictions for chunk ordering, for ancillary chunks
        /// </summary>
        public enum ChunkOrderingConstraint
        {
            /// <summary>
            /// No constraint, the chunk can go anywhere
            /// </summary>
            NONE,

            /// <summary>
            /// Before PLTE (palette) - and hence, also before IDAT
            /// </summary>
            BEFORE_PLTE_AND_IDAT,

            /// <summary>
            /// After PLTE (palette), but before IDAT
            /// </summary>
            AFTER_PLTE_BEFORE_IDAT,

            /// <summary>
            /// Before IDAT (before or after PLTE)
            /// </summary>
            BEFORE_IDAT,

            /// <summary>
            /// Does not apply.
            /// </summary>
            NA
        }

        /// <summary>
        /// 4 letters. The Id almost determines the concrete type (except for PngUKNOWN)
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Whether or not the chunk is critical.
        /// </summary>
        public readonly bool Critical;

        /// <summary>
        /// Whether or not the chunk is public.
        /// </summary>
        public readonly bool Public;

        /// <summary>
        /// Whether or not the chunk is safe to copy.
        /// </summary>
        public readonly bool Safe;

        /// <summary>
        /// Image basic info, mostly for some checks
        /// </summary>
        protected readonly ImageInfo ImgInfo;

        /// <summary>
        /// For writing. Queued chunks with high priority will be written as soon as possible
        /// </summary>
        public bool Priority { get; set; }

        /// <summary>
        /// Chunk group where it was read or writen
        /// </summary>
        public int ChunkGroup { get; set; }

        /// <summary>
        /// Only informational for chunk reads.
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// Only informational for chunk reads.
        /// </summary>
        public long Offset { get; set; }

        private static readonly Dictionary<string, Type> factoryMap = InitFactory();

        /// <summary>
        /// Constructs an empty chunk
        /// </summary>
        /// <param name="id"></param>
        /// <param name="imgInfo"></param>
        protected PngChunk(string id, ImageInfo imgInfo)
        {
            Id = id;
            ImgInfo = imgInfo;
            Critical = ChunkHelper.IsCritical(id);
            Public = ChunkHelper.IsPublic(id);
            Safe = ChunkHelper.IsSafeToCopy(id);
            Priority = false;
            ChunkGroup = -1;
            Length = -1;
            Offset = 0;
        }

        private static Dictionary<string, Type> InitFactory()
        {
            var f = new Dictionary<string, Type>
            {
                { ChunkHelper.IDAT, typeof(PngChunkIDAT) },
                { ChunkHelper.IHDR, typeof(PngChunkIHDR) },
                { ChunkHelper.PLTE, typeof(PngChunkPLTE) },
                { ChunkHelper.IEND, typeof(PngChunkIEND) },
                { ChunkHelper.tEXt, typeof(PngChunkTEXT) },
                { ChunkHelper.iTXt, typeof(PngChunkITXT) },
                { ChunkHelper.zTXt, typeof(PngChunkZTXT) },
                { ChunkHelper.bKGD, typeof(PngChunkBKGD) },
                { ChunkHelper.gAMA, typeof(PngChunkGAMA) },
                { ChunkHelper.pHYs, typeof(PngChunkPHYS) },
                { ChunkHelper.iCCP, typeof(PngChunkICCP) },
                { ChunkHelper.tIME, typeof(PngChunkTIME) },
                { ChunkHelper.tRNS, typeof(PngChunkTRNS) },
                { ChunkHelper.cHRM, typeof(PngChunkCHRM) },
                { ChunkHelper.sBIT, typeof(PngChunkSBIT) },
                { ChunkHelper.sRGB, typeof(PngChunkSRGB) },
                { ChunkHelper.hIST, typeof(PngChunkHIST) },
                { ChunkHelper.sPLT, typeof(PngChunkSPLT) },

                // extended
                { PngChunkOFFS.ID, typeof(PngChunkOFFS) },
                { PngChunkSTER.ID, typeof(PngChunkSTER) }
            };

            return f;
        }

        /// <summary>
        /// Registers a Chunk ID in the factory, to instantiate a given type
        /// </summary>
        /// <remarks>
        /// This can be called by client code to register additional chunk types
        /// </remarks>
        /// <param name="chunkId"></param>
        /// <param name="type">should extend PngChunkSingle or PngChunkMultiple</param>
        public static void FactoryRegister(string chunkId, Type type)
            => factoryMap.Add(chunkId, type);

        internal static bool IsKnown(string id)
            => factoryMap.ContainsKey(id);

        internal bool MustGoBeforePLTE()
            => GetOrderingConstraint() == ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;

        internal bool MustGoBeforeIDAT()
        {
            ChunkOrderingConstraint oc = GetOrderingConstraint();
            return oc == ChunkOrderingConstraint.BEFORE_IDAT
                || oc == ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT
                || oc == ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
        }

        internal bool MustGoAfterPLTE()
            => GetOrderingConstraint() == ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

        internal static PngChunk Factory(ChunkRaw rawChunk, ImageInfo info)
        {
            PngChunk chunk = FactoryFromId(ChunkHelper.ToString(rawChunk.IdBytes), info);
            chunk.Length = rawChunk.Length;
            chunk.ParseFromRaw(rawChunk);
            return chunk;
        }

        /// <summary>
        /// Creates one new blank chunk of the corresponding type, according to factoryMap (PngChunkUNKNOWN if not known)
        /// </summary>
        /// <param name="cid">Chunk Id</param>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static PngChunk FactoryFromId(string cid, ImageInfo info)
        {
            PngChunk? chunk = null;

            if (IsKnown(cid))
            {
                Type t = factoryMap[cid] ?? throw new Exception($"Type in factory was null: {cid}");
                ConstructorInfo? cons = t.GetConstructor([typeof(ImageInfo)]) ?? throw new Exception($"Constructor for type was null: {cid}");
                object o = cons.Invoke([info]);
                chunk = (PngChunk)o;
            }

            chunk ??= new PngChunkUNKNOWN(cid, info);

            return chunk;
        }

        public ChunkRaw CreateEmptyChunk(int len, bool alloc)
            => new(len, ChunkHelper.ToBytes(Id), alloc);

        public static T CloneChunk<T>(T chunk, ImageInfo info) where T : PngChunk
        {
            PngChunk cn = FactoryFromId(chunk.Id, info);
            if (cn.GetType() != chunk.GetType())
                throw new Exception($"Bad class cloning chunk: {cn.GetType()} {chunk.GetType()}");
            cn.CloneDataFromRead(chunk);
            return (T)cn;
        }

        internal void Write(Stream output)
        {
            ChunkRaw rawChunk = CreateRawChunk() ?? throw new Exception($"Chunk was null; {nameof(CreateRawChunk)} used in {nameof(Write)} failed for: {this}");
            rawChunk.WriteChunk(output);
        }

        /// <summary>
        /// Basic info: Id, length, Type name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"ChunkID={Id}, Length={Length}, Offset={Offset}, Name={GetType().Name}";

        /// <summary>
        /// Serialization. Creates a Raw chunk, ready for write, from this chunk content
        /// </summary>
        public abstract ChunkRaw? CreateRawChunk();

        /// <summary>
        /// Deserialization. Given a Raw chunk, just rad, fills this chunk content
        /// </summary>
        public abstract void ParseFromRaw(ChunkRaw c);

        /// <summary>
        /// Override to make a copy (normally deep) from other chunk
        /// </summary>
        /// <param name="other"></param>
        public abstract void CloneDataFromRead(PngChunk other);

        /// <summary>
        /// This is implemented in PngChunkMultiple/PngChunSingle
        /// </summary>
        /// <returns>Allows more than one chunk of this type in a image</returns>
        public abstract bool AllowsMultiple();

        /// <summary>
        /// Get the ordering constraint, determining where it can be placed.
        /// </summary>
        /// <returns></returns>
        public abstract ChunkOrderingConstraint GetOrderingConstraint();
    }
}
