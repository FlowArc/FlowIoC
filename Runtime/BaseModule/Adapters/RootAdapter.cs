using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.BaseModule.Adapters
{
    public class RootAdapter : MonoBehaviour
    {
        [SerializeField] private SerializedDictionary<string, ScriptableObject> _scriptableMap;
        [SerializeField] private SerializedDictionary<string, MonoBehaviour> _monoMap;

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);
        public T GetScriptable<T>(string assetName) where T : ScriptableObject => (T) _scriptableMap[assetName];
        public T GetMonoBehaviour<T>(string assetName) where T : MonoBehaviour => (T) _monoMap[assetName];
    }
}