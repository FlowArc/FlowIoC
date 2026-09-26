using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Root.Utils
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Every type in every loaded assembly, the framework's own included: a context the
        /// framework ships to be listed on a game's Root - PoolSubContext - has to resolve like the
        /// game's own, or the Root builds nothing for it. An assembly that cannot hand over all of
        /// its types gives up the ones it loaded rather than taking the whole scan down with it - a
        /// single broken reference used to stop every Root in the scene.
        /// </summary>
        private static List<Type> GetTypesInAllAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
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