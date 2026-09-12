#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer
{
    /// <summary>
    /// The whole of the Model Viewer's reflection. It reads one level at a time - the members of
    /// an object, the children of a value - and only when the window asks, which it does for an
    /// open row alone; so a closed row costs nothing and depth is the reader's choice rather than
    /// a cap. It never writes: a value changed from a window would skip the rules the Model
    /// exists to keep.
    ///
    /// A member that throws when read comes back as a fault rather than as an exception, so one
    /// bad getter does not take the window with it.
    /// </summary>
    internal class ModelMemberReader
    {
        /// <summary>How many children of one value are listed before the window offers more.</summary>
        public const int PAGE = 50;

        private const int STRING_LIMIT = 100;

        private const BindingFlags DECLARED =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Types drawn as one value rather than opened. The Unity structs are here because their
        /// ToString says everything their fields would, on one line.
        /// </summary>
        private static readonly HashSet<Type> LeafTypes = new HashSet<Type>
        {
            typeof(string), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid),
            typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Vector2Int), typeof(Vector3Int),
            typeof(Quaternion), typeof(Color), typeof(Color32), typeof(Rect), typeof(RectInt),
            typeof(Bounds), typeof(Matrix4x4)
        };

        private static readonly Dictionary<Type, string> Keywords = new Dictionary<Type, string>
        {
            {typeof(int), "int"}, {typeof(float), "float"}, {typeof(double), "double"}, {typeof(bool), "bool"},
            {typeof(string), "string"}, {typeof(long), "long"}, {typeof(byte), "byte"}, {typeof(short), "short"},
            {typeof(uint), "uint"}, {typeof(ulong), "ulong"}, {typeof(ushort), "ushort"}, {typeof(sbyte), "sbyte"},
            {typeof(char), "char"}, {typeof(decimal), "decimal"}, {typeof(object), "object"}, {typeof(void), "void"}
        };

        /// <summary>
        /// The members an object shows: a public field or property unless it is marked hidden, a
        /// non-public one only when it is marked shown. Declared type by declared type from the
        /// object's own up to <c>object</c>, so a base class's private marked field is found and
        /// listed after the derived class's members. Never a static member, an indexer, a
        /// property without a getter, or a compiler-generated member.
        /// </summary>
        public List<ModelNodeEVO> Members(object owner)
        {
            var nodes = new List<ModelNodeEVO>();

            if (owner == null) return nodes;

            for (Type type = owner.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(DECLARED))
                {
                    if (!IsShown(field, field.IsPublic)) continue;

                    FieldInfo read = field;
                    nodes.Add(Read(field.Name, field.FieldType, () => read.GetValue(owner)));
                }

                foreach (PropertyInfo property in type.GetProperties(DECLARED))
                {
                    MethodInfo getter = property.GetMethod;

                    if (getter == null || property.GetIndexParameters().Length > 0) continue;
                    if (!IsShown(property, getter.IsPublic)) continue;

                    PropertyInfo read = property;
                    nodes.Add(Read(property.Name, property.PropertyType, () => read.GetValue(owner)));
                }
            }

            return nodes;
        }

        /// <summary>
        /// What sits under a value: a dictionary's entries named by their keys, a sequence's items
        /// named by their index, anything else's members. At most <paramref name="limit"/> come
        /// back and <paramref name="hidden"/> says how many did not. A sequence that throws while
        /// it is walked - changed by another thread mid-read - comes back as one fault child.
        /// </summary>
        public List<ModelNodeEVO> Children(object value, Type declared, int limit, out int hidden)
        {
            hidden = 0;
            var nodes = new List<ModelNodeEVO>();

            if (value == null || value is string) return nodes;

            try
            {
                switch (value)
                {
                    case IDictionary dictionary:
                    {
                        Type valueType = GenericArgument(value.GetType(), typeof(IDictionary<,>), 1) ?? typeof(object);
                        int count = 0;

                        foreach (DictionaryEntry entry in dictionary)
                        {
                            if (nodes.Count < limit)
                            {
                                string name = entry.Key is string key ? key : Describe(entry.Key, entry.Key?.GetType());
                                nodes.Add(new ModelNodeEVO {Name = name, Value = entry.Value, Type = valueType});
                            }

                            count++;
                        }

                        hidden = count - nodes.Count;

                        return nodes;
                    }
                    case IEnumerable enumerable:
                    {
                        Type elementType = ElementType(value.GetType());
                        int index = 0;

                        foreach (object item in enumerable)
                        {
                            if (nodes.Count < limit)
                                nodes.Add(new ModelNodeEVO {Name = "[" + index + "]", Value = item, Type = elementType});

                            index++;
                        }

                        hidden = index - nodes.Count;

                        return nodes;
                    }
                    default:
                        return Members(value);
                }
            }
            catch (Exception exception)
            {
                nodes.Clear();
                hidden = 0;
                nodes.Add(new ModelNodeEVO {Name = "…", Type = declared, Fault = Describe(exception)});

                return nodes;
            }
        }

        /// <summary>
        /// Whether a type is drawn as one value. Primitives, enums, strings, the Unity structs, a
        /// Type, a delegate and any UnityEngine.Object are; a Nullable of one of those is; every
        /// other class, struct and collection opens.
        /// </summary>
        public bool IsLeaf(Type type)
        {
            if (type == null) return true;

            Type underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null) return IsLeaf(underlying);

            return type.IsPrimitive
                   || type.IsEnum
                   || LeafTypes.Contains(type)
                   || typeof(Type).IsAssignableFrom(type)
                   || typeof(Delegate).IsAssignableFrom(type)
                   || typeof(Object).IsAssignableFrom(type);
        }

        /// <summary>Whether a value has anything under it: not null, not a leaf, and for a collection not empty.</summary>
        public bool IsExpandable(object value)
        {
            if (value == null || value is string || IsLeaf(value.GetType())) return false;

            if (value is IEnumerable) return CountOf(value) != 0;

            return true;
        }

        /// <summary>
        /// What the value cell says. A leaf is written out; a collection says how many items it
        /// holds, because the type column already names it; an object says its ToString when it
        /// has one of its own, else its runtime type when that is not the declared one, else
        /// nothing - the arrow already says the row opens.
        /// </summary>
        public string Describe(object value, Type declared)
        {
            if (value == null) return "null";

            if (value is Object unityObject)
                return unityObject == null ? "null (destroyed)" : unityObject.name + " (" + TypeName(unityObject.GetType()) + ")";

            switch (value)
            {
                case string text: return Quote(text);
                case bool flag: return flag ? "true" : "false";
                case char character: return "'" + character + "'";
                case Enum: return value.ToString();
                case Type type: return TypeName(type);
                case Delegate handler: return TypeName(value.GetType()) + " · " + Count(handler.GetInvocationList().Length, "target");
                case ITuple tuple:
                {
                    var items = new string[tuple.Length];

                    for (int ii = 0; ii < tuple.Length; ii++)
                        items[ii] = Describe(tuple[ii], null);

                    return "(" + string.Join(", ", items) + ")";
                }
                case IEnumerable: return Count(CountOf(value), "item");
                case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            Type runtime = value.GetType();

            if (runtime.IsPrimitive) return Convert.ToString(value, CultureInfo.InvariantCulture);

            if (OverridesToString(runtime))
            {
                try
                {
                    return value.ToString();
                }
                catch (Exception exception)
                {
                    return "ToString threw " + Describe(exception);
                }
            }

            return declared == null || runtime != declared ? TypeName(runtime) : "";
        }

        /// <summary>
        /// A type name the way source spells it: no namespace, C# keywords for the primitives,
        /// <c>int?</c>, <c>int[]</c>, <c>List&lt;int&gt;</c>, <c>(int, Type)</c>.
        /// </summary>
        public string TypeName(Type type)
        {
            if (type == null) return "";

            if (Keywords.TryGetValue(type, out string keyword)) return keyword;

            Type underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null) return TypeName(underlying) + "?";

            if (type.IsArray) return TypeName(type.GetElementType()) + "[]";

            if (!type.IsGenericType) return type.Name;

            Type[] arguments = type.GetGenericArguments();
            var names = new string[arguments.Length];

            for (int ii = 0; ii < arguments.Length; ii++)
                names[ii] = TypeName(arguments[ii]);

            string joined = string.Join(", ", names);

            if (type.FullName != null && type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal))
                return "(" + joined + ")";

            string name = type.Name;
            int tick = name.IndexOf('`');

            if (tick >= 0) name = name.Substring(0, tick);

            return name + "<" + joined + ">";
        }

        private bool IsShown(MemberInfo member, bool isPublic)
        {
            if (member.Name.IndexOf('<') >= 0) return false;

            if (isPublic) return !member.IsDefined(typeof(HideInModelViewerAttribute), true);

            return member.IsDefined(typeof(ShowInModelViewerAttribute), true);
        }

        private ModelNodeEVO Read(string name, Type type, Func<object> read)
        {
            var node = new ModelNodeEVO {Name = name, Type = type};

            try
            {
                node.Value = read();
            }
            catch (Exception exception)
            {
                node.Fault = Describe(exception);
            }

            return node;
        }

        /// <summary>The exception a getter actually threw, unwrapped from reflection's own.</summary>
        private string Describe(Exception exception)
        {
            while (exception is TargetInvocationException invocation && invocation.InnerException != null)
                exception = invocation.InnerException;

            return exception.GetType().Name + ": " + exception.Message;
        }

        private string Quote(string text)
        {
            if (text.Length > STRING_LIMIT) text = text.Substring(0, STRING_LIMIT - 1) + "…";

            return "\"" + text.Replace("\n", "\\n") + "\"";
        }

        private string Count(int count, string noun) => count == 1 ? "1 " + noun : count + " " + noun + "s";

        /// <summary>
        /// How many items a collection holds, asked the cheap way where there is one: the
        /// non-generic ICollection, then a Count property - HashSet has the second and not the
        /// first - and only then a walk.
        /// </summary>
        private int CountOf(object value)
        {
            if (value is ICollection collection) return collection.Count;

            PropertyInfo count = value.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);

            if (count != null && count.PropertyType == typeof(int) && count.GetIndexParameters().Length == 0)
                return (int) count.GetValue(value);

            int walked = 0;

            foreach (object _ in (IEnumerable) value)
                walked++;

            return walked;
        }

        private bool OverridesToString(Type type)
        {
            MethodInfo toString = type.GetMethod("ToString", Type.EmptyTypes);
            Type declaring = toString?.DeclaringType;

            return declaring != null && declaring != typeof(object) && declaring != typeof(ValueType);
        }

        private Type ElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();

            return GenericArgument(type, typeof(IEnumerable<>), 0) ?? typeof(object);
        }

        /// <summary>
        /// The argument at <paramref name="index"/> of the generic interface <paramref name="definition"/>
        /// that <paramref name="type"/> implements, or null when it implements no such interface.
        /// </summary>
        private Type GenericArgument(Type type, Type definition, int index)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                return type.GetGenericArguments()[index];

            foreach (Type contract in type.GetInterfaces())
            {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == definition)
                    return contract.GetGenericArguments()[index];
            }

            return null;
        }
    }
}

#endif
