namespace Modules.CameraModule.Shared.Enums
{
    /// <summary>
    /// The game's cameras, by name. This file is the game's to extend - the card's Extend line
    /// says so, and an update asks before it touches the file once the game has edited it. Unity
    /// stores an enum as its number, so a new camera takes the next free number and a number a
    /// removed camera used is never given again.
    /// </summary>
    public enum CameraName
    {
        Menu = 0,
        Gameplay = 1
    }
}