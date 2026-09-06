#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// What one install did to one module, line by line. Both installers answer in this: giving a
    /// module its Shared assembly and giving it its public signal holder are the same shape of
    /// work - lay folders down, write an assembly, refresh the settings - and a reader wants the
    /// same account of either.
    ///
    /// The action names itself so the log says which install is talking.
    /// </summary>
    internal class ModuleInstallReport
    {
        private readonly List<string> _lines = new List<string>();
        private readonly string _action;

        internal ModuleInstallReport(string action)
        {
            _action = action;
        }

        public string AssemblyName { get; set; }
        public string Error { get; private set; }
        public bool Succeeded => string.IsNullOrEmpty(Error);

        /// <summary>
        /// Whether something was actually created or wired. The namespace settings file is left
        /// out on purpose: it is rewritten from the folder layout every time, the way it is for a
        /// module, so counting it would make every run look like it had work to do.
        /// </summary>
        public bool ChangedAnything { get; private set; }

        public void Fail(string reason) => Error = reason;

        public void CreatedFolders(string path) => Record($"Created {NamespaceUtility.GetUnityAssetPath(path)}");
        public void CreatedAssembly(string name) => Record($"Created {name}.asmdef");
        public void CreatedFile(string path) => Record($"Created {NamespaceUtility.GetUnityAssetPath(path)}");
        public void Referenced(string assemblyName) => Record($"{assemblyName} now references it");
        public void BoundInContext(string contextName) => Record($"Bound into {contextName}");
        public void WroteNamespaceSettings(string fileName) => _lines.Add($"Refreshed {fileName}");

        private void Record(string line)
        {
            ChangedAnything = true;
            _lines.Add(line);
        }

        public string Summary() => ChangedAnything
            ? string.Join("\n", _lines)
            : "Everything was already in place.";

        public void Log(string moduleName)
        {
            if (!Succeeded)
            {
                Debug.LogError($"<color=cyan>FlowIoC:</color> {_action} on '{moduleName}' - {Error}");
                return;
            }

            Debug.Log($"<color=cyan>FlowIoC:</color> {_action} on '{moduleName}'\n{Summary()}");
        }
    }
}

#endif
