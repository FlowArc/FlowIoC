#if UNITY_EDITOR

namespace FlowIoC.Editor.SetupModules
{
    /// <summary>What the startup hook does about the setup modules.</summary>
    internal enum SetupInstallDecision
    {
        /// <summary>Write nothing. Either the project has been here before, or nobody is watching.</summary>
        Stop,

        /// <summary>Install nothing, but record that the automatic install has had its turn.</summary>
        MarkOnly,

        /// <summary>Install the set, then record it.</summary>
        Install
    }

    /// <summary>
    /// The rule the startup hook follows, with nothing of Unity in it so that it can be read and
    /// tested on its own. Whether the marker is there, whether this is a batch run and whether the
    /// project already has modules are all answered before this is called.
    ///
    /// MarkOnly is the case worth understanding. A project with modules of its own has said what it
    /// is by having them, and writing six more into it would be rude. Marking it means the question
    /// is settled rather than asked again on every Editor launch - the reader can still take the
    /// set from the Help window if they want it.
    /// </summary>
    internal class SetupInstallRule
    {
        internal SetupInstallDecision For(bool markerPresent, bool isBatchMode, bool anyModulePresent)
        {
            if (markerPresent)
                return SetupInstallDecision.Stop;

            // A batch run has nobody to read the log and no business writing modules into the
            // workspace it was handed. It is not marked either, so an interactive session later
            // still gets its turn.
            if (isBatchMode)
                return SetupInstallDecision.Stop;

            return anyModulePresent ? SetupInstallDecision.MarkOnly : SetupInstallDecision.Install;
        }
    }
}

#endif
