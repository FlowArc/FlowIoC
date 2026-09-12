namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// What a message over the player connection is. Numbered like every enum in this project:
    /// the number is what travels, and a value inserted in the middle would change what an older
    /// player on the other end means.
    /// </summary>
    public enum PlayerLogKind
    {
        /// <summary>The player has connected and says what it is called.</summary>
        Hello = 0,

        /// <summary>One row the player would have recorded had it been the editor.</summary>
        Log = 1
    }
}
