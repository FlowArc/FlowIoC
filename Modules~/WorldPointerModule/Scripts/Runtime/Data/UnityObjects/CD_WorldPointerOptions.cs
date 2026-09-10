using Modules.WorldPointerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.WorldPointerModule.Data.UnityObjects
{
    /// <summary>A preset a prefab can carry. Authored in the Editor, constant at runtime.</summary>
    [CreateAssetMenu(fileName = "CD_WorldPointerOptions", menuName = "FlowIoC/WorldPointerModule/Data/CD_WorldPointerOptions")]
    public class CD_WorldPointerOptions : ScriptableObject
    {
        [SerializeField] private WorldPointerOptionsCVO _options = new WorldPointerOptionsCVO();

        public WorldPointerOptionsCVO Options => _options;
    }
}
