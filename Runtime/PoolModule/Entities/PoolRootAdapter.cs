using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Entities
{
    public class PoolRootAdapter : MonoBehaviour
    {
        [Header("Pool Groups")] 
        [SerializeField]
        private SerializedDictionary<string, PoolGroupCVO> _poolGroups = new();
        public SerializedDictionary<string, PoolGroupCVO> PoolGroups => _poolGroups;
    }
}
