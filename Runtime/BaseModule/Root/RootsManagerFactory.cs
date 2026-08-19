using System;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Root
{
    public static class RootsManagerFactory
    {
        private static IRootsManager _rootsManagerInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _rootsManagerInstance = null;

        public static IRootsManager GetRootsManager()
        {
            if (_rootsManagerInstance == null)
            {
                _rootsManagerInstance = new RootsManager();
                _rootsManagerInstance.Initialize();
            }

            return _rootsManagerInstance;
        }

        public static void ExecuteSafelyOnRootsManager(Action<IRootsManager> action)
        {
            if (_rootsManagerInstance != null)
            {
                action?.Invoke(_rootsManagerInstance);
            }
            else
            {
                FlowLogger.LogWarning(SystemLogType.Context,"RootsManager instance is not available for safe execution.");
            }
        }
    }
}