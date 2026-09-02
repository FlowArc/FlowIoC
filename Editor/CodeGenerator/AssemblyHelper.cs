#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
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
            var codeGenerationSettings = AssetDatabase.LoadAssetAtPath<ED_CodeGenerator>(CodeGeneratorStrings.CONFIG_PATH);

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

        /// <summary>
        /// The assembly a module name stands for, answered by the one class that knows the rules.
        /// This used to be a fifth hand-rolled copy of them, and it read one suffix off the end
        /// without asking what the rest was: "AaaScreenTest" lost its "Test" and became
        /// "Modules.AaaScreen.Test", while the asmdef the generator had just written next to it
        /// said "Modules.Aaa.Screen.Test". The lookup then found no assembly, and the screen's
        /// test Root was never placed in its scene.
        /// </summary>
        private static string ParseAssemblyName(string rawName) =>
            new ModuleAssemblyName().FromModuleName(rawName);
    }
}
#endif