using Modules.ResourceFlyModule.Services;

namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>
    /// One resource: the saved value as last told, what has not landed yet, how much of that is
    /// already on its way, and the counter showing it.
    /// </summary>
    internal class ResourceVO
    {
        public string Key;
        public int Saved;
        public int Pending;
        public int Flying;
        public IResourceFlyCounter Counter;

        public int Shown => Saved - Pending;

        /// <summary>What is pending and not yet on its way - all a new flight may take.</summary>
        public int Unflown => Pending - Flying;
    }
}
