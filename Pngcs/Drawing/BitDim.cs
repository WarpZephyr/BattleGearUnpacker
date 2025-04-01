using System.Runtime.CompilerServices;

namespace Pngcs.Drawing
{
    /// <summary>
    /// A bit scaler between types, moving the most significant bits around.
    /// </summary>
    internal static class BitDim
    {
        #region SByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(sbyte value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(byte value)
            => (sbyte)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(short value)
            => unchecked((sbyte)(value >> 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(ushort value)
            => unchecked((sbyte)(value >> 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(int value)
            => unchecked((sbyte)(value >> 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(uint value)
            => unchecked((sbyte)(value >> 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(long value)
            => unchecked((sbyte)(value >> 56));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ToSByte(ulong value)
            => unchecked((sbyte)(value >> 56));

        #endregion

        #region Byte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(sbyte value)
            => (byte)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(byte value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(short value)
            => unchecked((byte)(value >> 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(ushort value)
            => unchecked((byte)(value >> 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(int value)
            => unchecked((byte)(value >> 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(uint value)
            => unchecked((byte)(value >> 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(long value)
            => unchecked((byte)(value >> 56));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToByte(ulong value)
            => unchecked((byte)(value >> 56));

        #endregion

        #region Int16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(sbyte value)
            => unchecked((short)(value << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(byte value)
            => unchecked((short)(value << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(short value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(ushort value)
            => (short)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(int value)
            => unchecked((short)(value >> 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(uint value)
            => unchecked((short)(value >> 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(long value)
            => unchecked((short)(value >> 48));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ToInt16(ulong value)
            => unchecked((short)(value >> 48));

        #endregion

        #region UInt16

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(sbyte value)
            => unchecked((ushort)(value << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte value)
            => unchecked((ushort)(value << 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(short value)
            => (ushort)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ushort value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(int value)
            => unchecked((ushort)(value >> 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(uint value)
            => unchecked((ushort)(value >> 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(long value)
            => unchecked((ushort)(value >> 48));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(ulong value)
            => unchecked((ushort)(value >> 48));

        #endregion

        #region Int32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(sbyte value)
            => unchecked(value << 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(byte value)
            => unchecked(value << 24);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(short value)
            => unchecked(value << 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ushort value)
            => unchecked(value << 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(int value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(uint value)
            => (int)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(long value)
            => unchecked((int)(value >> 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt32(ulong value)
            => unchecked((int)(value >> 32));

        #endregion

        #region UInt32

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(sbyte value)
            => unchecked((uint)(value << 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte value)
            => unchecked((uint)(value << 24));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(short value)
            => unchecked((uint)(value << 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ushort value)
            => unchecked((uint)(value << 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(int value)
            => (uint)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(uint value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(long value)
            => unchecked((uint)(value >> 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(ulong value)
            => unchecked((uint)(value >> 32));

        #endregion

        #region Int64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(sbyte value)
            => unchecked(value << 56);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(byte value)
            => unchecked(value << 56);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(short value)
            => unchecked(value << 48);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(ushort value)
            => unchecked(value << 48);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(int value)
            => unchecked(value << 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(uint value)
            => unchecked(value << 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(long value)
            => value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToInt64(ulong value)
            => (long)value;

        #endregion

        #region UInt64

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(sbyte value)
            => unchecked((ulong)(value << 56));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte value)
            => unchecked((ulong)(value << 56));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(short value)
            => unchecked((ulong)(value << 48));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ushort value)
            => unchecked((ulong)(value << 48));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(int value)
            => unchecked((ulong)(value << 32));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(uint value)
            => unchecked(value << 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(long value)
            => (ulong)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(ulong value)
            => value;

        #endregion
    }
}
