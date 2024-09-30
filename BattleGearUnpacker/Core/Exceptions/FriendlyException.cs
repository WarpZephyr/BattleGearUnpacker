using System;

namespace BattleGearUnpacker.Core.Exceptions
{
    /// <summary>
    /// Thrown to describe an error to a user in a more friendly way.
    /// </summary>
    internal class FriendlyException : Exception
    {
        public FriendlyException(string message) : base(message) { }
    }
}
