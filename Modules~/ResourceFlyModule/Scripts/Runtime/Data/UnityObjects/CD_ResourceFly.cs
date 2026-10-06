using Modules.ResourceFlyModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.ResourceFlyModule.Data.UnityObjects
{
    /// <summary>The look of every flight. Filed on ResourceFlyServiceRoot's adapter.</summary>
    [CreateAssetMenu(fileName = "CD_ResourceFly", menuName = "FlowIoC/ResourceFlyModule/Data/CD_ResourceFly")]
    public class CD_ResourceFly : ScriptableObject
    {
        public ResourceFlyOptionsCVO Options = new();
    }
}
