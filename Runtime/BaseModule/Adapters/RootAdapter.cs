using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.BaseModule.Adapters
{
    /// <summary>
    /// The one component beside a Root that hands the module what the scene holds for it: its
    /// assets by name, and the scene components it drives. Two slots of each - the module's own,
    /// and what it shares with every other module through ISharedDataModel.
    /// </summary>
    public class RootAdapter : MonoBehaviour
    {
        /// <summary>
        /// Protected rather than private on purpose: a module reads its own assets by name through
        /// GetScriptable, and only an adapter of its own - one that is a registry and has to walk
        /// every entry, the way the save module's is - derives from this and sees the whole map.
        /// </summary>
        [SerializeField] protected SerializedDictionary<string, ScriptableObject> _scriptableMap;

        /// <summary>
        /// The assets this Root shares with the scene. Whatever is filed here is handed to the
        /// RootsManager when the Root registers, and any injectable then reads it through
        /// ISharedDataModel - so a shared asset is filed once, on one Root, rather than dragged
        /// onto every reader's adapter. The slot says the asset is common, not who produces it: a
        /// test Root files a ready-made RD_ asset here when the producer is not in the scene.
        /// </summary>
        [SerializeField] [Tooltip("Assets any module reads through ISharedDataModel. File a shared asset once, on one Root.")]
        private SerializedDictionary<string, ScriptableObject> _sharedScriptableMap;

        [SerializeField] private SerializedDictionary<string, MonoBehaviour> _monoMap;

        /// <summary>
        /// The scene components this Root shares, filed and read exactly like the shared assets:
        /// once, on one Root, through ISharedDataModel.GetMonoBehaviour.
        /// </summary>
        [SerializeField] [Tooltip("Scene components any module reads through ISharedDataModel. File a shared component once, on one Root.")]
        private SerializedDictionary<string, MonoBehaviour> _sharedMonoMap;

        /// <summary>The Shared Scriptables slot as serialized - null until something is filed in it.</summary>
        internal IReadOnlyDictionary<string, ScriptableObject> SharedScriptables => _sharedScriptableMap;

        /// <summary>The Shared Monos slot as serialized - null until something is filed in it.</summary>
        internal IReadOnlyDictionary<string, MonoBehaviour> SharedMonoBehaviours => _sharedMonoMap;

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);

        /// <summary>
        /// The asset filed under that name in either slot - the module's own first, then what it
        /// shares - or null with an error that names the asset and the Root. The miss is reported
        /// here rather than left to the caller, because the fix is the same wherever it is
        /// noticed: one drag onto this adapter in the Inspector.
        /// </summary>
        public T GetScriptable<T>(string assetName) where T : ScriptableObject
        {
            if (TryFind(_scriptableMap, assetName, out T filed) || TryFind(_sharedScriptableMap, assetName, out filed))
                return filed;

            ReportMissing("Asset", assetName, typeof(T).Name);
            return null;
        }

        public T GetMonoBehaviour<T>() where T : MonoBehaviour => GetMonoBehaviour<T>(typeof(T).Name);

        /// <summary>
        /// The component filed under that name in either slot, or null with the same report a
        /// missing asset gets. It used to throw a KeyNotFoundException from inside the framework,
        /// which pointed at this class rather than at the adapter that was never filled.
        /// </summary>
        public T GetMonoBehaviour<T>(string componentName) where T : MonoBehaviour
        {
            if (TryFind(_monoMap, componentName, out T filed) || TryFind(_sharedMonoMap, componentName, out filed))
                return filed;

            ReportMissing("Component", componentName, typeof(T).Name);
            return null;
        }

        private bool TryFind<TFiled, TEntry>(SerializedDictionary<string, TEntry> map, string entryName, out TFiled filed)
            where TFiled : TEntry
            where TEntry : Object
        {
            filed = null;

            if (map != null && map.TryGetValue(entryName, out TEntry entry) && entry is TFiled typed)
                filed = typed;

            return filed != null;
        }

        private void ReportMissing(string kind, string entryName, string typeName)
        {
            FlowLogger.LogError(SystemLogType.Context,
                "<b><color=#FF6666>► " + kind + " is not filed on the RootAdapter!</color></b>\n" +
                "<b><color=#FF6666>► " + kind + ":</color><color=#FFEFD5> " + entryName + " (" + typeName + ")</color></b>\n" +
                "<b><color=#FF6666>► Root:</color><color=#FFEFD5> " + gameObject.name + "</color></b>\n" +
                "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null. Drag it onto " +
                "this Root's RootAdapter, under that name.</color></b>",
                context: this);
        }
    }
}