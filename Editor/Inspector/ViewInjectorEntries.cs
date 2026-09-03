#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Keeps the injector's list in step with the views on the object: one entry per IView, and
    /// no entry for a view that has been removed.
    ///
    /// It works through the SerializedProperty rather than the list itself, so adding a view is
    /// undoable and lands on the prefab or the scene that actually holds the object - which the
    /// hand-rolled dirty marking this replaces got wrong on a prefab instance.
    /// </summary>
    internal class ViewInjectorEntries
    {
        private readonly List<IView> _views = new List<IView>();

        internal void Sync(ViewInjector injector, SerializedProperty entries)
        {
            injector.GetComponents(_views);

            Prune(entries);
            Add(entries);
        }

        /// <summary>
        /// Backwards, so removing one entry does not move the entries still to be looked at.
        /// </summary>
        private void Prune(SerializedProperty entries)
        {
            for (int i = entries.arraySize - 1; i >= 0; i--)
            {
                Object view = ViewOf(entries.GetArrayElementAtIndex(i));

                if (view == null || !_views.Contains(view as IView))
                    entries.DeleteArrayElementAtIndex(i);
            }
        }

        private void Add(SerializedProperty entries)
        {
            foreach (IView view in _views)
            {
                if (IndexOf(entries, (Object) view) >= 0)
                    continue;

                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);

                // Every field is written: Unity fills a new element with a copy of the one before
                // it, so an entry added after a view that names a Root would inherit that name.
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);

                entry.FindPropertyRelative(nameof(ViewInjectorData.View)).objectReferenceValue = (Object) view;
                entry.FindPropertyRelative(nameof(ViewInjectorData.AutoRegister)).boolValue = true;
                entry.FindPropertyRelative(nameof(ViewInjectorData.InjectableView)).boolValue = false;
                entry.FindPropertyRelative(nameof(ViewInjectorData.ContextSource)).enumValueIndex =
                    (int) ViewContextSource.BubbleUp;
                entry.FindPropertyRelative(nameof(ViewInjectorData.SelectedRoot)).objectReferenceValue = null;
                entry.FindPropertyRelative(nameof(ViewInjectorData.RootName)).stringValue = string.Empty;
                entry.FindPropertyRelative(nameof(ViewInjectorData.IsRegistered)).boolValue = false;
            }
        }

        private int IndexOf(SerializedProperty entries, Object view)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (ViewOf(entries.GetArrayElementAtIndex(i)) == view)
                    return i;
            }

            return -1;
        }

        private Object ViewOf(SerializedProperty entry)
        {
            return entry.FindPropertyRelative(nameof(ViewInjectorData.View)).objectReferenceValue;
        }
    }
}

#endif
