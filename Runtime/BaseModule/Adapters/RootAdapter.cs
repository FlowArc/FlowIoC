using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.BaseModule.Adapters
{
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

        /// <summary>The Shared Scriptables slot as serialized - null until something is filed in it.</summary>
        internal IReadOnlyDictionary<string, ScriptableObject> SharedScriptables => _sharedScriptableMap;

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

            FlowLogger.LogError(SystemLogType.Context,
                "<b><color=#FF6666>► Asset is not filed on the RootAdapter!</color></b>\n" +
                "<b><color=#FF6666>► Asset:</color><color=#FFEFD5> " + assetName + " (" + typeof(T).Name + ")</color></b>\n" +
                "<b><color=#FF6666>► Root:</color><color=#FFEFD5> " + gameObject.name + "</color></b>\n" +
                "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null. Drag the asset onto " +
                "this Root's RootAdapter, under that name.</color></b>",
                context: this);
            return null;
        }

        public T GetMonoBehaviour<T>(string assetName) where T : MonoBehaviour => (T) _monoMap[assetName];

        private bool TryFind<T>(SerializedDictionary<string, ScriptableObject> map, string assetName, out T filed)
            where T : ScriptableObject
        {
            filed = null;

            if (map != null && map.TryGetValue(assetName, out ScriptableObject asset) && asset is T typed)
                filed = typed;

            return filed != null;
        }
    }
}