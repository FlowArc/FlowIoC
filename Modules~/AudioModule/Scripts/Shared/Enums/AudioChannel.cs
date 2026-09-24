namespace Modules.AudioModule.Shared.Enums
{
    /// <summary>
    /// Where a sound plays: a mixer group, and a set of voices of its own with its own limit.
    /// Music follows the player's music setting; the other three follow the sound setting.
    /// </summary>
    public enum AudioChannel
    {
        Music = 0,
        Sfx = 1,
        Ui = 2,
        Ambient = 3
    }
}
