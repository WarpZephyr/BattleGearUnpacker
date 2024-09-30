namespace BattleGearUnpacker.Core
{
    internal static class MathHelper
    {
        public static int BinaryAlign(int num, int alignment)
            => (num + (--alignment)) & ~alignment;

        public static long BinaryAlign(long num, long alignment)
            => (num + (--alignment)) & ~alignment;
    }
}
