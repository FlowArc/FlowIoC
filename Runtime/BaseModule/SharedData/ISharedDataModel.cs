using UnityEngine;

namespace FlowIoC.BaseModule.SharedData
{
    /// <summary>
    /// The assets filed as shared on any Root in the scene, readable from any injectable. A Root
    /// files them in its adapter's Shared Scriptables and the RootsManager files them here when
    /// the Root registers, so what a reader gets is the instance one Root holds - never a second
    /// copy dragged onto the reader's own adapter. Filing happens at Awake, before any binding
    /// phase, so a PostConstruct may read one.
    /// </summary>
    public interface ISharedDataModel
    {
        /// <summary>The shared asset filed under the type's name.</summary>
        T GetScriptable<T>() where T : ScriptableObject;

        /// <summary>
        /// The shared asset filed under that name, or null with an error naming the asset. The miss
        /// is reported here rather than left to the caller, because the fix is the same wherever it
        /// is noticed: one filing, in the Shared Scriptables of one Root.
        /// </summary>
        T GetScriptable<T>(string assetName) where T : ScriptableObject;
    }
}
