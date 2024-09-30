namespace BattleGearUnpacker.Core.Exceptions
{
    /// <summary>
    /// Thrown when there is an XML value parsing error.
    /// </summary>
    internal class XmlValueParseException : ValueParseException
    {
        /// <summary>
        /// Creates a new <see cref="XmlValueParseException"/>, and throws an error with the specified message.
        /// </summary>
        /// <param name="message">The error message to throw.</param>
        public XmlValueParseException(string message)
            : base(message) { }
    }
}
