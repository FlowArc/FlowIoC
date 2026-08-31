#if UNITY_EDITOR

using System;
using UnityEngine.EventSystems;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// Which input module a scene FlowIoC authors should put beside its EventSystem.
    ///
    /// StandaloneInputModule reads through the legacy UnityEngine.Input, which throws in a project
    /// whose active input handling is the Input System alone - and that is what a new Unity 6
    /// project is set to. So the Input System's own module is preferred whenever the package is
    /// there, and the legacy one is the fallback for a project that kept the old handling.
    ///
    /// The type is looked up by name rather than referenced: FlowIoC does not depend on
    /// com.unity.inputsystem, and a reference would make the Editor assembly stop compiling in
    /// every project that does not have it.
    /// </summary>
    internal class UiInputModuleType
    {
        internal const string InputSystemTypeName =
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        private readonly Func<string, Type> _lookup;

        internal UiInputModuleType() : this(Type.GetType)
        {
        }

        internal UiInputModuleType(Func<string, Type> lookup)
        {
            _lookup = lookup;
        }

        internal Type Resolve() => _lookup(InputSystemTypeName) ?? typeof(StandaloneInputModule);
    }
}

#endif
