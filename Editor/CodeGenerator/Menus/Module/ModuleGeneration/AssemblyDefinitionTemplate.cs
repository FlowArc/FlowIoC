#if UNITY_EDITOR
using System.Collections.Generic;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// The asmdef body every generated module assembly is written from.
    ///
    /// This used to be two near-identical string literals inside ModuleGenerator, one for an
    /// assembly with a reference and one for an assembly without. A module can now need more than
    /// one - its own Shared assembly and the Shared assembly of the module it lives in - so the
    /// reference list is built rather than interpolated, and the literal exists once.
    /// </summary>
    internal class AssemblyDefinitionTemplate
    {
        private const string FLOW_IOC_ASSEMBLY = "FlowIoC";

        /// <summary>
        /// Every module assembly references FlowIoC, so it is always first. Anything null, empty
        /// or already listed is dropped: callers pass references they may or may not have found -
        /// a module with no Shared folder, a parent module that publishes nothing - and asking
        /// each of them to filter first only spreads the same check around.
        /// </summary>
        public string Build(string assemblyName, IEnumerable<string> referenceAssemblies)
        {
            var references = new List<string> {FLOW_IOC_ASSEMBLY};

            if (referenceAssemblies != null)
            {
                foreach (string reference in referenceAssemblies)
                {
                    if (string.IsNullOrEmpty(reference) || references.Contains(reference)) continue;

                    references.Add(reference);
                }
            }

            string referenceLines = string.Join(",\n", references.ConvertAll(reference => $"    \"{reference}\""));

            return $@"{{
  ""name"": ""{assemblyName}"",
  ""references"": [
{referenceLines}
  ],
  ""includePlatforms"": [],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": false,
  ""precompiledReferences"": [],
  ""autoReferenced"": true,
  ""defineConstraints"": [],
  ""versionDefines"": [],
  ""noEngineReferences"": false
}}";
        }
    }
}
#endif
