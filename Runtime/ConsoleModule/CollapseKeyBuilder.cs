namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The key the console's Collapse mode folds rows on. Two logs fold together when they say
    /// the same thing, from the same place, on the same channel - the channel is part of it
    /// because the same sentence on two channels is two different things to whoever is reading.
    /// A plain class rather than a static so it can be handed to a test.
    /// </summary>
    public class CollapseKeyBuilder
    {
        public int Build(string message, string stackTrace, string channel)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (message == null ? 0 : message.GetHashCode());
                hash = hash * 31 + (stackTrace == null ? 0 : stackTrace.GetHashCode());
                hash = hash * 31 + (channel == null ? 0 : channel.GetHashCode());
                return hash;
            }
        }
    }
}