#if UNITY_EDITOR

using FlowIoC.Editor.AgentRules;
using FlowIoC.Editor.ModuleScanner;
using UnityEngine;

namespace FlowIoC.Editor.ModuleCards
{
    /// <summary>
    /// Brings every card and the directory back in line with the project, by running the two
    /// checks the scanner already owns. Going through the scanner rather than writing files
    /// directly is what keeps one answer to "is this card current": the panel, Fix All and this
    /// refresher all read the same checks.
    ///
    /// The pipeline it builds holds those two checks and nothing else. Running the whole scanner
    /// after every compile would repair asmdefs and settings files nobody asked about.
    /// </summary>
    internal class ModuleCardsRefresher
    {
        private readonly ModuleCardsAutoSync _autoSync = new ModuleCardsAutoSync();

        /// <summary>
        /// The triggers' entry point: refresh unless this project asked to be left alone.
        /// </summary>
        internal void Run()
        {
            // A batch run has no business writing into the workspace it was handed, and nobody to
            // read what it wrote.
            if (Application.isBatchMode) return;

            if (_autoSync.IsOff(new ProjectRoot().Resolve())) return;

            Refresh();
        }

        /// <summary>
        /// Refresh whatever the switch says. This is what the Agent Scanner's Sync button calls,
        /// because pressing it is the asking the switch was about.
        /// </summary>
        internal void Refresh()
        {
            var pipeline = new ModuleCheckPipeline(
                new IModuleCheck[] {new ModuleCardCheck()},
                new IProjectCheck[] {new ModuleDirectoryCheck()});

            new ModuleRepair(pipeline).FixAll();
        }
    }
}

#endif