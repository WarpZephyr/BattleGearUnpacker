using System;

namespace BattleGearUnpacker.Unpacker.Exceptions
{
    /// <summary>
    /// Thrown to describe an error to a user in a more friendly way.
    /// </summary>
    internal class FriendlyException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="FriendlyException"/>, and throws an error with the specified message.
        /// </summary>
        /// <param name="message">The error message to throw.</param>
        public FriendlyException(string message) : base(message) { }
    }
}
