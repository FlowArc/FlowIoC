using UnityEngine.Scripting;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The project's modules by name, one <c>const string</c> each: <c>FlowModule.PlayerModule</c>
    /// is the string <c>"PlayerModule"</c>. The Flow Console is what reads it today - a module's
    /// channel is its name - but the constant identifies the module, not a log, and anything that
    /// keys something by module reads the same one.
    ///
    /// The parts are generated, one per module at
    /// <c>Scripts/Generated/FlowModule.&lt;Module&gt;.cs</c> and compiled into this assembly by the
    /// asmref beside them. This part is the package's own and declares the one name that belongs
    /// to no module, so that <c>Default</c> exists in a project that has generated nothing yet and
    /// <see cref="FlowLogChannels"/> always has something to read.
    ///
    /// Every public <c>const string</c> on the class is a channel, and a <c>&lt;Name&gt;Color</c>
    /// or <c>&lt;Name&gt;Profile</c> field beside one says how its rows are drawn. The table reads
    /// them by reflection, which is what the attribute is for: a stripped player would otherwise be
    /// free to drop fields nothing references by name.
    /// </summary>
    [Preserve]
    public static partial class FlowModule
    {
        /// <summary>The channel for a log that belongs to no module: a test fixture, a probe, a script outside `Modules/`.</summary>
        public const string Default = "Default";
    }
}
