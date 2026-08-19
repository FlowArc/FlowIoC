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

        private static List<Type> GetTypesInAllAssemblies()
        {
            Assembly[] assemblies = GetAssemblies();
            List<Type> typeList = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .ToList();

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