namespace Modules.AudioModule.Enums
{
    /// <summary>What a channel does with a new sound when every one of its voices is playing.</summary>
    public enum AudioSteal
    {
        /// <summary>The voice that started first stops and plays the new sound.</summary>
        Oldest = 0,

        /// <summary>The new sound is dropped.</summary>
        Reject = 1
    }
}
