using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Root.Utils
{
    public static class AssemblyExtensions
    {
        private static Assembly[] GetAssemblies()
        {
            Assembly currentAssembly = typeof(Context).Assembly;

            Assembly[] assemblyList = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => x != currentAssembly)
                .ToArray();

            return assemblyList;
        }

        /// <summary>
        /// Every type in every loaded assembly but the framework's own. An assembly that cannot
        /// hand over all of its types gives up the ones it loaded rather than taking the whole
        /// scan down with it - a single broken reference used to stop every Root in the scene.
        /// </summary>
        private static List<Type> GetTypesInAllAssemblies()
        {
            Assembly[] assemblies = GetAssemblies();
            List<Type> typeList = new List<Type>();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;

                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                for (int ii = 0; ii < types.Length; ii++)
                {
                    if (types[ii] != null)
                        typeList.Add(types[ii]);
                }
            }

            return typeList;
        }

        public static List<Type> GetAllContextTypes()
        {
            List<Type> contextList = GetTypesInAllAssemblies()
                .Where(type => type.IsSubclassOf(typeof(Context)))
                .ToList();

            return contextList;
        }

        public static List<Type> GetAllRootTypes()
        {
            List<Type> rootList = GetTypesInAllAssemblies()
                .Where(type => type.IsSubclassOf(typeof(RootBase)))
                .ToList();

            return rootList;
        }
    }
}