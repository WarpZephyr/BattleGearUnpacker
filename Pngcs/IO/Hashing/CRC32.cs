namespace Pngcs.IO.Hashing
{
    // Based on http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net
    public class CRC32
    {
        private const uint defaultPolynomial = 0xedb88320;
        private const uint defaultSeed = 0xffffffff;
        private static uint[]? defaultTable;

        private uint _hash;
        private readonly uint _seed;
        private readonly uint[] _table;

        public CRC32() : this(defaultPolynomial, defaultSeed) { }

        public CRC32(uint polynomial, uint seed)
        {
            _table = InitializeTable(polynomial);
            _seed = seed;
            _hash = seed;
        }

        public void Update(byte[] buffer)
        {
            Update(buffer, 0, buffer.Length);
        }

        public void Update(byte[] buffer, int start, int length)
        {
            for (int i = 0, j = start; i < length; i++, j++)
            {
                _hash = unchecked(_hash >> 8 ^ _table[buffer[j] ^ _hash & 0xff]);
            }
        }

        public uint GetValue()
        {
            return ~_hash;
        }

        public void Reset()
        {
            _hash = _seed;
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == defaultPolynomial && defaultTable != null)
                return defaultTable;

            uint[] createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                    {
                        entry = entry >> 1 ^ polynomial;
                    }
                    else
                    {
                        entry >>= 1;
                    }
                }
                createTable[i] = entry;
            }

            if (polynomial == defaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

    }
}
