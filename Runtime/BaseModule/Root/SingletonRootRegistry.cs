using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.BaseModule.Root
{
    internal static class SingletonRootRegistry
    {
        private static readonly Dictionary<Type, RootBase> _instances = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _instances.Clear();

        public static bool TryClaim(RootBase root)
        {
            Type type = root.GetType();

            if (_instances.TryGetValue(type, out RootBase existing) && existing != null && existing != root)
                return false;

            _instances[type] = root;
            return true;
        }

        public static bool IsClaimedBy(RootBase root)
        {
            return _instances.TryGetValue(root.GetType(), out RootBase existing) && existing == root;
        }

        public static void Release(RootBase root)
        {
            Type type = root.GetType();

            if (_instances.TryGetValue(type, out RootBase existing) && existing == root)
                _instances.Remove(type);
        }
    }
}
