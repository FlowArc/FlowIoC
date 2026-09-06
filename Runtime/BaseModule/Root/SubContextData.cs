using System;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.BaseModule.Root
{
    /// <summary>
    /// One sub-context as a Root lists it. The screen fields are the exception to this struct
    /// being about contexts in general: they are the configuration a screen context declares in
    /// code, which the Root may override for its own registration. They are prefixed so another
    /// kind of sub-context can add its own without a collision.
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
        /// Whether the five screen fields below replace what the screen context declares. Off on
        /// every entry that predates the feature, so an untouched scene keeps its behaviour.
        /// </summary>
        public bool OverrideScreen;

        /// <summary>Which screen manager this registration belongs to.</summary>
        public int ScreenManagerId;

        /// <summary>How far up the stack the screen is drawn. A higher layer covers a lower one.</summary>
        public int ScreenLayer;

        /// <summary>What kind of surface this is - a screen in its own right, or a popup over one.</summary>
        public ScreenTag ScreenTag;

        /// <summary>Whether the screen plays its own animation when it opens, instead of appearing.</summary>
        public bool ScreenHasShowAnimation;

        /// <summary>Whether the screen plays its own animation when it closes.</summary>
        public bool ScreenHasHideAnimation;
    }
}