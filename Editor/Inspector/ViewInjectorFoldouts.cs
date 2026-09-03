#if UNITY_EDITOR

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

        internal bool IsExpanded(int injectorInstanceId, string viewTypeName)
            => SessionState.GetBool(Key(injectorInstanceId, viewTypeName), true);

        internal void SetExpanded(int injectorInstanceId, string viewTypeName, bool expanded)
            => SessionState.SetBool(Key(injectorInstanceId, viewTypeName), expanded);

        private string Key(int injectorInstanceId, string viewTypeName)
            => $"{Prefix}{injectorInstanceId}.{viewTypeName}";
    }
}

#endif
