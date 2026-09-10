using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using UnityEngine;

namespace FlowIoC.BaseModule.Root
{
    public interface IRoot
    {
        string Name { get; }

        /// <summary>
        /// What this Root files as shared - the Shared Scriptables of its adapter - or null when
        /// it has none. The RootsManager reads it at registration.
        /// </summary>
        IReadOnlyDictionary<string, ScriptableObject> SharedScriptables { get; }

        void StartContext(bool forceToStart = false);
        void InitializeSubContexts();
        IContext GetContext();
        Stack<IContext> GetSubContexts();
        Stack<IContext> GetAllContexts();
        void Setup(bool forceToSetup = false);
        void Launch(bool forceToLaunch = false);
    }
}