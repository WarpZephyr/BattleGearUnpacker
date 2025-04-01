using System.Runtime.CompilerServices;

namespace Pngcs.Drawing
{
    /// <summary>
    /// A color dimension scaler between types.
    /// </summary>
    internal static class ColorDim
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToL8(byte red, byte green, byte blue)
            => (byte)((red + green + blue) / 3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToL8(ushort red, ushort green, ushort blue)
            => BitDim.ToByte(ToL16(red, green, blue));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToL16(ushort red, ushort green, ushort blue)
            => (ushort)((red + green + blue) / 3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToL16(byte red, byte green, byte blue)
            => BitDim.ToUInt16(ToL8(red, green, blue));
    }
}
