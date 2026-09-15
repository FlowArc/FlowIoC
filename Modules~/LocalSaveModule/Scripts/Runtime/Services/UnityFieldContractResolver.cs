using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// Teaches Newtonsoft which members Unity would have saved, so swapping JsonUtility out changed
    /// what the save can carry without changing what it carries.
    ///
    /// Left alone, Newtonsoft takes public properties as well as fields, which on a ScriptableObject
    /// means name, hideFlags and everything else the engine hangs off Object - none of it the
    /// player's data. So the walk stops at the first engine type, and only fields Unity itself would
    /// have serialized are kept.
    ///
    /// A field that points at another Object is dropped rather than followed: an asset reference is
    /// a pointer, and a pointer means nothing to the session that reads the file back.
    /// </summary>
    internal class UnityFieldContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = new List<MemberInfo>();

            for (Type type = objectType; type != null && !IsEngineType(type); type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public
                                                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (IsSaved(field))
                        members.Add(field);
                }
            }

            return members;
        }

        /// <summary>
        /// Where a type stops being the game's and starts being the engine's. Object, Component,
        /// MonoBehaviour and ScriptableObject all live in the same assembly, so one check covers
        /// every base a saved asset can have.
        /// </summary>
        private static bool IsEngineType(Type type) =>
            type == typeof(object) || type.Assembly == typeof(ScriptableObject).Assembly;

        private static bool IsSaved(FieldInfo field)
        {
            if (field.IsStatic || field.IsLiteral || field.IsInitOnly)
                return false;

            if (field.IsDefined(typeof(NonSerializedAttribute), false))
                return false;

            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false))
                return false;

            return !typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType);
        }
    }
}
