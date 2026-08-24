using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Root
{
    // Owned by RootsManager rather than held statically: the manager already has the
    // lifetime a singleton claim needs - one instance per play session, recreated by
    // RootsManagerFactory - so the registry needs no domain reload reset of its own.
    internal class SingletonRootRegistry
    {
        private readonly Dictionary<Type, RootBase> _instances = new();

        public bool TryClaim(RootBase root)
        {
            Type key = GetSingletonKey(root);

            if (_instances.TryGetValue(key, out RootBase existing) && existing != null && existing != root)
                return false;

            _instances[key] = root;
            return true;
        }

        public void Release(RootBase root)
        {
            Type key = GetSingletonKey(root);

            if (_instances.TryGetValue(key, out RootBase existing) && existing == root)
                _instances.Remove(key);
        }

        // The claim belongs to the type that declares itself a singleton, not to the concrete
        // type in the scene. Without this, DebugAudioRoot : AudioRoot would claim its own slot
        // and both Roots would survive.
        private Type GetSingletonKey(RootBase root)
        {
            Type type = root.GetType();

            while (type != null && type.BaseType != null)
            {
                if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(SingletonRoot<>))
                    return type;

                type = type.BaseType;
            }

            return root.GetType();
        }
    }
}