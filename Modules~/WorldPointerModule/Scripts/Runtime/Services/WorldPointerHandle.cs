using System;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// What Register hands back and Unregister takes. A value: a copy disposed twice is harmless,
    /// because the service ignores an id it no longer holds. IsValid says the handle was issued,
    /// not that it is still registered. Ids start at 1 so that default is never valid.
    /// </summary>
    public readonly struct WorldPointerHandle : IDisposable
    {
        internal readonly int Id;
        internal readonly IWorldPointerService Owner;

        internal WorldPointerHandle(int id, IWorldPointerService owner)
        {
            Id = id;
            Owner = owner;
        }

        public bool IsValid => Owner != null && Id > 0;

        /// <summary>The same as handing the handle to Unregister.</summary>
        public void Dispose() => Owner?.Unregister(this);
    }
}
