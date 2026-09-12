#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>
    /// Every loaded type by its simple name, read once from the domain and kept for the life of
    /// the planner. The plan asks it twice per identifier - is the old name also somebody else's,
    /// is the new name already taken - and walking every assembly for each question would make a
    /// preview that follows keystrokes cost a domain scan per key.
    ///
    /// An assembly that will not list its types answers with the ones it can, which is what
    /// ReflectionTypeLoadException carries; a dynamic assembly has nothing on disk to collide with
    /// and is skipped.
    /// </summary>
    internal class LoadedTypeNames
    {
        private Dictionary<string, List<TypeHomeEVO>> _bySimpleName;

        internal IReadOnlyList<TypeHomeEVO> Named(string simpleName)
        {
            _bySimpleName ??= Read();

            return _bySimpleName.TryGetValue(simpleName, out List<TypeHomeEVO> homes) ? homes : new List<TypeHomeEVO>();
        }

        private Dictionary<string, List<TypeHomeEVO>> Read()
        {
            var read = new Dictionary<string, List<TypeHomeEVO>>(StringComparer.Ordinal);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;

                foreach (Type type in TypesOf(assembly))
                {
                    if (type == null || string.IsNullOrEmpty(type.FullName)) continue;

                    if (!read.TryGetValue(type.Name, out List<TypeHomeEVO> homes))
                        read[type.Name] = homes = new List<TypeHomeEVO>();

                    homes.Add(new TypeHomeEVO {FullName = type.FullName, AssemblyName = assembly.GetName().Name});
                }
            }

            return read;
        }

        private static IEnumerable<Type> TypesOf(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException partial)
            {
                return partial.Types ?? Array.Empty<Type>();
            }
        }
    }
}
#endif
