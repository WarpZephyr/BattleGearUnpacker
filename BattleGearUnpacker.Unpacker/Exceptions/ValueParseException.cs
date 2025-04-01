namespace BattleGearUnpacker.Unpacker.Exceptions
{
    /// <summary>
    /// Thrown when there is a value parsing error.
    /// </summary>
    internal class ValueParseException : FriendlyException
    {
        /// <summary>
        /// Creates a new <see cref="ValueParseException"/>, and throws an error with the specified message.
        /// </summary>
        /// <param name="message">The error message to throw.</param>
        public ValueParseException(string message)
            : base(message) { }
    }
}
