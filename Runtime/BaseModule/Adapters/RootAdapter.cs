using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.BaseModule.Adapters
{
    public class RootAdapter : MonoBehaviour
    {
        [SerializeField] private SerializedDictionary<string, ScriptableObject> _scriptableMap;
        [SerializeField] private SerializedDictionary<string, MonoBehaviour> _monoMap;

        /// <summary>
        /// Every asset on this adapter, by the name it was filed under. A module that acts on all
        /// of them - persisting them, say - needs to walk the map rather than ask for one by name.
        /// </summary>
        public IReadOnlyDictionary<string, ScriptableObject> Scriptables => _scriptableMap;

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);
        public T GetScriptable<T>(string assetName) where T : ScriptableObject => (T) _scriptableMap[assetName];
        public T GetMonoBehaviour<T>(string assetName) where T : MonoBehaviour => (T) _monoMap[assetName];
    }
}