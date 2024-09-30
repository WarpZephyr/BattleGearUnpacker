using System.Runtime.CompilerServices;

namespace BattleGearUnpacker.Core
{
    /// <summary>
    /// A bit packer for packing and unpacking bits.
    /// </summary>
    internal static class BitPacker
    {
        #region Pack

        /// <summary>
        /// Pack a value into a <see cref="byte"/> with the given position from the left.
        /// </summary>
        /// <param name="pack">The value to pack into.</param>
        /// <param name="value">The value to pack.</param>
        /// <param name="bitPosition">The position to pack at.</param>
        /// <returns>The packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LeftPackByte(byte pack, byte value, int bitPosition)
            => (byte)(unchecked(value << bitPosition) | pack);

        /// <summary>
        /// Pack a value into a <see cref="ushort"/> with the given position from the left.
        /// </summary>
        /// <param name="pack">The value to pack into.</param>
        /// <param name="value">The value to pack.</param>
        /// <param name="bitPosition">The position to pack at.</param>
        /// <returns>The packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort LeftPackUInt16(ushort pack, ushort value, int bitPosition)
            => (ushort)(unchecked(value << bitPosition) | pack);

        /// <summary>
        /// Pack a value into a <see cref="uint"/> with the given position from the left.
        /// </summary>
        /// <param name="pack">The value to pack into.</param>
        /// <param name="value">The value to pack.</param>
        /// <param name="bitPosition">The position to pack at.</param>
        /// <returns>The packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint LeftPackUInt32(uint pack, uint value, int bitPosition)
            => unchecked(value << bitPosition) | pack;

        /// <summary>
        /// Pack a value into a <see cref="ulong"/> with the given position from the left.
        /// </summary>
        /// <param name="pack">The value to pack into.</param>
        /// <param name="value">The value to pack.</param>
        /// <param name="bitPosition">The position to pack at.</param>
        /// <returns>The packed value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LeftPackUInt64(ulong pack, ulong value, int bitPosition)
            => unchecked(value << bitPosition) | pack;

        #endregion

        #region Unpack

        /// <summary>
        /// Unpack a left packed value from the given position.
        /// </summary>
        /// <param name="unpack">The value to unpack from.</param>
        /// <param name="bitCount">The number of bits the value to unpack takes.</param>
        /// <param name="bitPosition">The position to unpack from.</param>
        /// <returns>The unpacked value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte LeftUnpackByte(byte unpack, int bitCount, int bitPosition)
            => (byte)((unpack >>> bitPosition) & ((2U << (bitCount - 1)) - 1));

        /// <summary>
        /// Unpack a left packed value from the given position.
        /// </summary>
        /// <param name="unpack">The value to unpack from.</param>
        /// <param name="bitCount">The number of bits the value to unpack takes.</param>
        /// <param name="bitPosition">The position to unpack from.</param>
        /// <returns>The unpacked value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort LeftUnpackUInt16(ushort unpack, int bitCount, int bitPosition)
            => (ushort)((unpack >>> bitPosition) & ((2U << (bitCount - 1)) - 1));

        /// <summary>
        /// Unpack a left packed value from the given position.
        /// </summary>
        /// <param name="unpack">The value to unpack from.</param>
        /// <param name="bitCount">The number of bits the value to unpack takes.</param>
        /// <param name="bitPosition">The position to unpack from.</param>
        /// <returns>The unpacked value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint LeftUnpackUInt32(uint unpack, int bitCount, int bitPosition)
            => (unpack >>> bitPosition) & ((2U << (bitCount - 1)) - 1);

        /// <summary>
        /// Unpack a left packed value from the given position.
        /// </summary>
        /// <param name="unpack">The value to unpack from.</param>
        /// <param name="bitCount">The number of bits the value to unpack takes.</param>
        /// <param name="bitPosition">The position to unpack from.</param>
        /// <returns>The unpacked value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LeftUnpackUInt64(ulong unpack, int bitCount, int bitPosition)
            // Shift down to get the unmasked value.
            // - Use unsigned right shift to not carry a sign bit.
            // Generate a mask by using bitshifts for a power of 2 and subtracting 1 after.
            // - Cannot use 1 << bitCount, must be 2 << (bitCount - 1) in case the mask becomes 0.
            // - Must define constant as a long or the 32-bit limit will be hit easily.
            // Mask the raw value to kill all unwanted bits.
            => (unpack >>> bitPosition) & ((2UL << (bitCount - 1)) - 1);

        #endregion
    }
}
