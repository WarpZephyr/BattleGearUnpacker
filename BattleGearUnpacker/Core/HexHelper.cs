using BattleGearUnpacker.Core.Exceptions;

namespace BattleGearUnpacker.Core
{
    internal static class HexHelper
    {
        public static byte[] ToBytes(string str)
        {
            string cleanInput = CleanHexInput(str);
            if (cleanInput.Length % 2 != 0)
            {
                throw new ValueParseException("Hex length was not even.");
            }

            int length = cleanInput.Length / 2;
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Convert.ToByte(cleanInput.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        public static string ToHex(byte[] bytes)
        {
            string str = string.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                str += bytes[i].ToString("X2");
            }
            return str;
        }

        public static bool IsHex(char c)
            => (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');

        public static bool IsHex(string str)
        {
            if (str.Length % 2 != 0)
            {
                return false;
            }

            foreach (char c in str)
            {
                if (!IsHex(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static string CleanHexInput(string input)
        {
            var chars = new List<char>();
            foreach (char c in input)
            {
                if (IsHex(c))
                {
                    chars.Add(char.ToUpper(c));
                }
            }

            return new string(chars.ToArray());
        }
    }
}
