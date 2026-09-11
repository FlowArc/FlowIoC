using UnityEngine;

namespace FlowIoC.BaseModule.SharedData
{
    /// <summary>
    /// What the Roots in the scene file as shared, readable from any injectable: assets from
    /// their adapters' Shared Scriptables, scene components from their Shared Monos. The
    /// RootsManager files them here when a Root registers, so what a reader gets is the instance
    /// one Root holds - never a second copy dragged onto the reader's own adapter. Filing happens
    /// at Awake, before any binding phase, so a PostConstruct may read one.
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

        /// <summary>The shared scene component filed under the type's name.</summary>
        T GetMonoBehaviour<T>() where T : MonoBehaviour;

        /// <summary>
        /// The shared scene component filed under that name, or null with an error naming it - the
        /// same report, the same one fix: a filing in the Shared Monos of one Root.
        /// </summary>
        T GetMonoBehaviour<T>(string componentName) where T : MonoBehaviour;
    }
}