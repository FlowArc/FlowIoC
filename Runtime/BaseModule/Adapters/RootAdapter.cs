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

        [SerializeField] private SerializedDictionary<string, MonoBehaviour> _monoMap;

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);

        /// <summary>
        /// The asset filed under that name, or null with an error that names the asset and the
        /// Root. The miss is reported here rather than left to the caller, because the fix is the
        /// same wherever it is noticed: one drag onto this adapter in the Inspector.
        /// </summary>
        public T GetScriptable<T>(string assetName) where T : ScriptableObject
        {
            if (_scriptableMap != null && _scriptableMap.TryGetValue(assetName, out ScriptableObject asset)
                && asset is T filed)
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
    }
}
