using System.Collections.Generic;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.RootsContexts;
using Modules.LocalSaveModule.Services;
using UnityEngine;

namespace Modules.LocalSaveModule.Models
{
    /// <summary>
    /// The registry and the restore pass.
    ///
    /// PostConstruct is the whole startup path, and it is PostConstruct rather than Setup on
    /// purpose: PostConstruct runs during the binding pass, so a Root at a low enough
    /// InitializeOrder finishes restoring before any other module's own PostConstruct reads the
    /// assets. Setup would be a frame too late, and a Command later still - the signal that would
    /// drive one is not dispatched until Launch.
    /// </summary>
    public class LocalSaveModel : ILocalSaveModel, IConstructable
    {
        [Inject(nameof(LocalSaveServiceContext))] private GameObject _root { get; set; }
        [Inject] private ILocalSaveService _service { get; set; }

        private readonly Dictionary<string, ScriptableObject> _persisted = new();

        private string _password;

#if UNITY_EDITOR
        private readonly EditorAssetSnapshots _snapshots = new();
#endif

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            ReadRegistry();

#if UNITY_EDITOR
            // Put back straight away as well as at the end: a build starts every asset from what
            // was built into it, and a run in the Editor starts from the asset's file the same way
            // rather than from whatever an earlier run left in memory.
            _snapshots.Capture(_persisted.Values);
            _snapshots.Restore();
#endif

            _service.BeginSession(_password);

            foreach (KeyValuePair<string, ScriptableObject> entry in _persisted)
            {
                if (_service.Has(entry.Key))
                    _service.LoadInto(entry.Key, entry.Value);
            }
        }

        public void Deconstruct() => _persisted.Clear();

        public void Save(string assetName)
        {
            if (!_persisted.TryGetValue(assetName, out ScriptableObject asset))
                return;

            _service.Save(assetName, asset);
            _service.Flush();
        }

        public void SaveAll()
        {
            foreach (KeyValuePair<string, ScriptableObject> entry in _persisted)
                _service.Save(entry.Key, entry.Value);

            _service.Flush();
        }

#if UNITY_EDITOR
        public void RestoreEditorAssets() => _snapshots.Restore();
#endif

        /// <summary>
        /// The adapter on the Root is the registry: whatever is filed there is persisted, and a
        /// module joins in by dropping its asset on it rather than by writing any code. It is the
        /// module's own LocalSaveRootAdapter, because walking the whole map is something only a
        /// registry does - a plain RootAdapter hands out one asset by name and nothing else.
        /// The password is read with it: the adapter is where the module is told things.
        /// </summary>
        private void ReadRegistry()
        {
            _persisted.Clear();
            _password = null;

            var adapter = _root.GetComponent<LocalSaveRootAdapter>();
            if (adapter == null)
                return;

            _password = adapter.Password;

            foreach (KeyValuePair<string, ScriptableObject> entry in adapter.Scriptables)
            {
                if (entry.Value != null)
                    _persisted[entry.Key] = entry.Value;
            }
        }
    }
}
