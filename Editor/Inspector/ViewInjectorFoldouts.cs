#if UNITY_EDITOR

using FlowIoC.BaseModule.Injectable.Components;
using UnityEditor;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Whether a view's entry on the injector is expanded. Keyed by object and by view, so two
    /// views on the same object fold separately and the same view on two objects does too.
    ///
    /// Entries start open: an object usually carries one view, and folding the only thing an
    /// inspector has to say would be a click for nothing. SessionState rather than EditorPrefs,
    /// because a fold is a convenience of the moment and not a project setting.
    /// </summary>
    internal class ViewInjectorFoldouts
    {
        private const string Prefix = "FlowIoC.ViewInjector.Entry.";

        private readonly SessionObjectId _ids = new();

        internal bool IsExpanded(ViewInjector injector, string viewTypeName)
            => SessionState.GetBool(Key(injector, viewTypeName), true);

        internal void SetExpanded(ViewInjector injector, string viewTypeName, bool expanded)
            => SessionState.SetBool(Key(injector, viewTypeName), expanded);

        private string Key(ViewInjector injector, string viewTypeName)
            => $"{Prefix}{_ids.For(injector)}.{viewTypeName}";
    }
}

#endif
