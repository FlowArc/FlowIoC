#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.ModulePanels
{
    /// <summary>
    /// The drag a number row's label carries, the way the Inspector's does: the pointer turns into
    /// the slide arrow over the name, and dragging left or right moves the value. A panel's fields
    /// are drawn without a label of their own, so without this a number could only be typed.
    ///
    /// The pace is Unity's unless the panel names one: Unity's grows with the value's distance from
    /// zero, a named pace is the value moved per pixel whatever the value is - 0.1 on an int is one
    /// step every ten pixels. The value stays inside the field's own [Min] or [Range], and it is
    /// written through the SerializedProperty, so undo and dirtying are the panel's Apply as for a
    /// typed value. Escape during a drag puts the value back.
    /// </summary>
    internal class ModulePanelNumberDrag
    {
        private const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly int _hint = "ModulePanelNumberDrag".GetHashCode();
        private readonly Dictionary<string, NumberBoundsEVO> _bounds = new Dictionary<string, NumberBoundsEVO>();

        /// <summary>What one drag remembers between its events, kept by Unity against the control.</summary>
        private class DragStateEVO
        {
            internal double Start;
            internal float Travel;
            internal float Sensitivity;
        }

        /// <summary>
        /// The drag over a label, for a float or an int; any other property is left alone. The
        /// control id is taken on every event, so the ids of the fields after it do not shift
        /// between layout and repaint.
        /// </summary>
        internal void Handle(Rect zone, SerializedProperty property, float? pace)
        {
            if (!IsNumber(property))
                return;

            int id = GUIUtility.GetControlID(_hint, FocusType.Passive, zone);

            if (!GUI.enabled)
                return;

            Event current = Event.current;
            EditorGUIUtility.AddCursorRect(zone, MouseCursor.SlideArrow, id);

            bool isInteger = property.propertyType == SerializedPropertyType.Integer;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (current.button != 0 || !zone.Contains(current.mousePosition))
                        return;

                    DragStateEVO started = State(id);
                    started.Start = isInteger ? property.longValue : property.doubleValue;
                    started.Travel = 0f;
                    started.Sensitivity = SensitivityFor(started.Start, isInteger, pace);

                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(1);
                    current.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        return;

                    DragStateEVO dragged = State(id);
                    dragged.Travel += current.delta.x;

                    double value = ValueAt(dragged.Start, dragged.Travel, dragged.Sensitivity, isInteger);
                    Write(property, BoundsOf(property).Clamp(value), isInteger);
                    current.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        return;

                    GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    current.Use();
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl != id || current.keyCode != KeyCode.Escape)
                        return;

                    Write(property, State(id).Start, isInteger);
                    GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    current.Use();
                    break;
            }
        }

        /// <summary>
        /// The value moved per pixel: the panel's pace when it names one, otherwise Unity's own,
        /// read once at the start of the drag the way the Inspector reads it.
        /// </summary>
        internal float SensitivityFor(double start, bool isInteger, float? pace)
        {
            if (pace.HasValue)
                return Mathf.Abs(pace.Value);

            double distance = Math.Pow(Math.Abs(start), 0.5);

            return isInteger
                ? (float) Math.Max(1d, distance * 0.03d)
                : (float) (Math.Max(1d, distance) * 0.03d);
        }

        /// <summary>
        /// The value after a travel in pixels. An int moves a whole step once the travel has covered
        /// one, never on half; a float is rounded to the pace's own precision, so a drag at 0.1
        /// lands on 1.3 and not on 1.2999999.
        /// </summary>
        internal double ValueAt(double start, float travel, float sensitivity, bool isInteger)
        {
            double moved = travel * (double) sensitivity;

            if (isInteger)
                return start + Math.Truncate(moved);

            int decimals = sensitivity > 0f
                ? Mathf.Clamp(-(int) Math.Floor(Math.Log10(sensitivity)), 0, 15)
                : 15;

            return Math.Round(start + moved, decimals);
        }

        /// <summary>
        /// The [Range] or [Min] on the field a property path ends at, walked from the type that
        /// holds it: through nested classes and into the elements of an array or a list, whose
        /// attribute Unity applies to every element. A path that names no field is open.
        /// </summary>
        internal NumberBoundsEVO BoundsOf(Type root, string propertyPath)
        {
            FieldInfo field = FieldOf(root, propertyPath);

            if (field == null)
                return NumberBoundsEVO.Open;

            RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

            if (range != null)
                return new NumberBoundsEVO(range.min, range.max);

            MinAttribute min = field.GetCustomAttribute<MinAttribute>();

            return min != null ? new NumberBoundsEVO(min.min, null) : NumberBoundsEVO.Open;
        }

        private NumberBoundsEVO BoundsOf(SerializedProperty property)
        {
            UnityEngine.Object target = property.serializedObject.targetObject;

            if (target == null)
                return NumberBoundsEVO.Open;

            Type root = target.GetType();
            string key = root.FullName + "/" + property.propertyPath;

            if (!_bounds.TryGetValue(key, out NumberBoundsEVO bounds))
            {
                bounds = BoundsOf(root, property.propertyPath);
                _bounds[key] = bounds;
            }

            return bounds;
        }

        private static bool IsNumber(SerializedProperty property) =>
            property != null
            && !property.hasMultipleDifferentValues
            && (property.propertyType == SerializedPropertyType.Float
                || property.propertyType == SerializedPropertyType.Integer);

        private static DragStateEVO State(int id) =>
            (DragStateEVO) GUIUtility.GetStateObject(typeof(DragStateEVO), id);

        private static void Write(SerializedProperty property, double value, bool isInteger)
        {
            if (isInteger)
                property.longValue = (long) value;
            else
                property.doubleValue = value;

            GUI.changed = true;
        }

        private static FieldInfo FieldOf(Type root, string propertyPath)
        {
            Type type = root;
            FieldInfo field = null;
            string[] parts = propertyPath.Split('.');

            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index] == "Array" && index + 1 < parts.Length && parts[index + 1].StartsWith("data["))
                {
                    type = ElementOf(type);
                    index++;

                    if (type == null)
                        return null;

                    continue;
                }

                field = FindField(type, parts[index]);

                if (field == null)
                    return null;

                type = field.FieldType;
            }

            return field;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(name, Declared);

                if (field != null)
                    return field;
            }

            return null;
        }

        private static Type ElementOf(Type collection)
        {
            if (collection.IsArray)
                return collection.GetElementType();

            if (collection.IsGenericType && collection.GetGenericTypeDefinition() == typeof(List<>))
                return collection.GetGenericArguments()[0];

            return null;
        }
    }
}

#endif
