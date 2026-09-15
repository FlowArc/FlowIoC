using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// What a save file is allowed to name.
    ///
    /// Carrying a polymorphic field means writing its concrete type into the file, and reading one
    /// back means building whatever type the file names. A save file sits in the player's own
    /// folder and can be edited, so the set of types a name may resolve to is kept to the project's
    /// own assemblies: nothing from the runtime or the engine is ever constructed from the file.
    /// </summary>
    internal class LocalSaveTypeBinder : ISerializationBinder
    {
        private static readonly string[] RefusedPrefixes =
        {
            "System", "mscorlib", "netstandard", "Unity", "UnityEngine", "UnityEditor", "Mono"
        };

        private readonly DefaultSerializationBinder _default = new DefaultSerializationBinder();

        public void BindToName(Type serializedType, out string assemblyName, out string typeName) =>
            _default.BindToName(serializedType, out assemblyName, out typeName);

        public Type BindToType(string assemblyName, string typeName)
        {
            Type type = _default.BindToType(assemblyName, typeName);

            if (IsAllowed(type))
                return type;

            throw new JsonSerializationException("[LocalSave] refused to build '" + typeName + "' from '"
                                                 + assemblyName
                                                 + "': a save file may only name the project's own types.");
        }

        private static bool IsAllowed(Type type)
        {
            string assembly = type.Assembly.GetName().Name;

            foreach (string prefix in RefusedPrefixes)
            {
                if (assembly.StartsWith(prefix, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}
