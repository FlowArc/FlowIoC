namespace Modules.LoadingModule.Shared.Enums
{
    /// <summary>
    /// What the player sees while a set runs. Silent shows nothing and still announces. A screen
    /// that waits on its own data shows the LoadingSpinner prefab inside itself instead; 2 was
    /// Overlay and is not reused.
    /// </summary>
    public enum LoadingPresentation
    {
        Silent = 0,
        Fullscreen = 1
    }
}
