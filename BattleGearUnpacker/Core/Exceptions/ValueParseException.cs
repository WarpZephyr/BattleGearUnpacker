namespace BattleGearUnpacker.Core.Exceptions
{
    /// <summary>
    /// Thrown when there is a value parsing error.
    /// </summary>
    internal class ValueParseException : FriendlyException
    {
        public ValueParseException(string message)
            : base(message) { }
    }
}
