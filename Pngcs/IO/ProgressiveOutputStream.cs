using System;
using System.IO;

namespace Pngcs.IO
{
    /// <summary>
    /// A stream that outputs to memory and allows to flush fragments every 'size' bytes to some other destination.
    /// </summary>
    internal abstract class ProgressiveOutputStream : MemoryStream
    {
        private readonly int _size;
        public long CountFlushed { get; private set; }

        public ProgressiveOutputStream(int size)
        {
            if (size < 8)
                throw new ArgumentException($"Size for {nameof(ProgressiveOutputStream)} is too small: {size}", nameof(size));

            _size = size;
        }

        public override void Close()
        {
            Flush();
            base.Close();
        }

        public override void Flush()
        {
            base.Flush();
            CheckFlushBuffer(true);
        }

        public override void Write(byte[] b, int off, int len)
        {
            base.Write(b, off, len);
            CheckFlushBuffer(false);
        }

        public void Write(byte[] b)
        {
            Write(b, 0, b.Length);
            CheckFlushBuffer(false);
        }

        /// <summary>
        /// If it's time to flush data (or if forced==true) calls abstract method FlushBuffer() and cleans those bytes from own buffer.
        /// </summary>
        private void CheckFlushBuffer(bool forced)
        {
            int count = (int)Position;
            byte[] buffer = GetBuffer();
            while (forced || count >= _size)
            {
                int nb = _size;
                if (nb > count)
                    nb = count;

                if (nb == 0)
                    return;

                FlushBuffer(buffer, nb);
                CountFlushed += nb;
                int bytesleft = count - nb;
                count = bytesleft;
                Position = count;

                if (bytesleft > 0)
                    Array.Copy(buffer, nb, buffer, 0, bytesleft);
            }
        }

        protected abstract void FlushBuffer(byte[] b, int n);
    }
}
