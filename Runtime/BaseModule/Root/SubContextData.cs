using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// One sub-context as a Root lists it. What a particular kind of sub-context is configured
    /// with - a screen's layer, a module's pool groups - is not a field here but the entry's
    /// Settings, whose class the context names, so a Root knows nothing about screens or pools.
    /// </summary>
    [Serializable]
    public struct SubContextData
    {
        /// <summary>
        /// The script asset the context is declared in, and the entry's truth at edit time. A
        /// plain class is not a UnityEngine.Object and cannot be referenced directly, so what is
        /// held is the MonoScript that compiles to it - which is what puts a real guid in the
        /// scene or prefab, so that renaming the class, moving its file, or deleting the module
        /// it lives in is something the project can see rather than a name that quietly stops
        /// resolving.
        ///
        /// Typed as UnityEngine.Object because MonoScript lives in UnityEditor and this struct is
        /// compiled into the player. A build nulls the reference, which is why ContextFullName
        /// stays below and is what runtime reads.
        /// </summary>
        public UnityEngine.Object ContextScript;

        /// <summary>
        /// The context's full name. Runtime resolves the type from this and from nothing else,
        /// because MonoScript.GetClass is editor-only. It is a cache of what ContextScript says,
        /// rewritten from the script whenever the inspector or a generator touches the entry, so
        /// a class renamed inside its file repairs itself rather than breaking every Root at once.
        /// </summary>
        public string ContextFullName;

        public string ContextName;

        public bool AutoSetup;
        public bool IsTest;

        /// <summary>
        /// What this Root says about the context it lists, of the class the context names through
        /// ISubContextConfigurable&lt;TSettings&gt;. Null means nothing configured: the entry
        /// predates its context taking settings, or its context takes none.
        /// </summary>
        [SerializeReference] public SubContextSettingsCVO Settings;

        // The screen override as it was stored before it moved into the entry's settings. Read
        // once, by RootBase.OnAfterDeserialize, which moves a ticked override into a
        // ScreenSubContextSettingsCVO and clears these. Kept for one release so a scene saved
        // under the old shape keeps its screens where they were; remove with that migration.
        [SerializeField, HideInInspector, FormerlySerializedAs("OverrideScreen")]
        internal bool LegacyOverrideScreen;

        [SerializeField, HideInInspector, FormerlySerializedAs("ScreenManagerId")]
        internal int LegacyScreenManagerId;

        [SerializeField, HideInInspector, FormerlySerializedAs("ScreenLayer")]
        internal int LegacyScreenLayer;

        /// <summary>The ScreenTag, read as the int Unity stores an enum as.</summary>
        [SerializeField, HideInInspector, FormerlySerializedAs("ScreenTag")]
        internal int LegacyScreenTag;

        [SerializeField, HideInInspector, FormerlySerializedAs("ScreenHasShowAnimation")]
        internal bool LegacyScreenHasShowAnimation;

        [SerializeField, HideInInspector, FormerlySerializedAs("ScreenHasHideAnimation")]
        internal bool LegacyScreenHasHideAnimation;
    }
}
