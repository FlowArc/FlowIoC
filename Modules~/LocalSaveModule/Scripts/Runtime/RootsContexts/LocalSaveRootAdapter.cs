using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using UnityEngine;

namespace Modules.LocalSaveModule.RootsContexts
{
    /// <summary>
    /// The adapter on LocalSaveRoot is the registry: whatever is filed on it is persisted, and a
    /// module joins in by dropping its asset there rather than by writing any code. That is why
    /// this one, alone, hands out the whole map - an ordinary module reads its own assets by name
    /// through GetScriptable and never needs to see what else is filed.
    ///
    /// The password is the module's one setting, and it sits here for the same reason the
    /// registry does: the Root is the module's one presence in the scene, so what the module
    /// needs told is told on the Root.
    /// </summary>
    public class LocalSaveRootAdapter : RootAdapter
    {
        [SerializeField]
        [Tooltip("Encrypts the save file with this password. Leave it empty for a plain file a developer can read. "
                 + "It keeps a player from editing the file in a text editor, not from a determined one.")]
        private string _password;

        /// <summary>Every asset on this adapter, by the name it was filed under.</summary>
        public IReadOnlyDictionary<string, ScriptableObject> Scriptables => _scriptableMap;

        /// <summary>The password the save file is encrypted with, or empty for a plain file.</summary>
        public string Password => _password;
    }
}