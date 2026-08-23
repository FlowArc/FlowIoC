#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Contexts;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    public static class AssemblyHelper
    {
        public static List<Type> GetAllTypesFromAssemblies()
        {
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();

            var result = new List<Type>();

            foreach (var assembly in assemblyList)
            {
                if (assembly.FullName.StartsWith("Unity.") ||
                    assembly.FullName.StartsWith("UnityEngine.") ||
                    assembly.FullName.StartsWith("UnityEditor.") ||
                    assembly.FullName.StartsWith("System.") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("netstandard"))
                {
                    continue;
                }

                try
                {
                    var assemblyTypes = assembly.GetTypes();
                    var contextTypes = new List<Type>();

                    foreach (var type in assemblyTypes)
                    {
                        if (!type.IsPublic || type.IsAbstract || type.IsInterface)
                            continue;

                        bool isValidType = false;

                        try
                        {
                            isValidType = typeof(IContext).IsAssignableFrom(type);
                        }
                        catch
                        {
                        }

                        if (!isValidType && type.Namespace != null)
                        {
                            isValidType = type.Namespace.Contains("Module") ||
                                          type.Namespace.Contains("Modules");
                        }

                        if (isValidType)
                        {
                            contextTypes.Add(type);
                        }
                    }

                    if (contextTypes.Count > 0)
                    {
                        result.AddRange(contextTypes);
                        //Debug.Log($"Added {contextTypes.Count} context-related types from assembly: {assembly.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error processing assembly {assembly.FullName}: {ex.Message}");
                }
            }


            //Debug.Log($"Total types collected: {result.Count}");

            return result;
        }

        public static List<Type> GetAllTypesFromAssemblies(string assemblyName)
        {
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
            var codeGenerationSettings = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettings>(CodeGeneratorStrings.CONFIG_PATH);

            string transformedName = ParseAssemblyName(assemblyName);

            var mainAssembly = assemblyList.FirstOrDefault(x => x.FullName.StartsWith(transformedName));
            var result = new List<Type>();

            if (mainAssembly == null)
            {
                Debug.LogError($"No assembly found that starts with '{transformedName}'. Check your assembly name.");
                return result;
            }

            result.AddRange(mainAssembly.GetTypes());

            // Null checks that were never reached while this settings asset was looked up at a path
            // that does not exist: the list is absent from assets serialized by older versions, and
            // an entry goes null when the asmdef it pointed at is deleted.
            if (codeGenerationSettings != null && codeGenerationSettings.AssemblyDefinitions != null)
            {
                foreach (var assemblyDefinitionAsset in codeGenerationSettings.AssemblyDefinitions)
                {
                    if (assemblyDefinitionAsset == null)
                        continue;

                    var assembly = assemblyList.FirstOrDefault(x => x.FullName.StartsWith(assemblyDefinitionAsset.name));
                    if (assembly == null)
                        continue;

                    result.AddRange(assembly.GetTypes());
                }
            }

            return result;
        }

        private static string ParseAssemblyName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName))
                return string.Empty;


            if (rawName.EndsWith("Screen", StringComparison.OrdinalIgnoreCase))
            {
                var core = rawName.Substring(0, rawName.Length - "Screen".Length);
                return "Modules." + core + ".Screen";
            }

            else if (rawName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
            {
                var core = rawName.Substring(0, rawName.Length - "Test".Length);
                return "Modules." + core + ".Test";
            }

            else if (rawName.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
            {
                var core = rawName.Substring(0, rawName.Length - "Module".Length);
                return "Modules." + core;
            }

            return "Modules." + rawName;
        }
    }
}
#endif