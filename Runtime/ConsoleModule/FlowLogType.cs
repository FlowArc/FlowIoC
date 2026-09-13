using UnityEngine.Scripting;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The project's channels, one <c>const string</c> each. The parts are generated: one per
    /// module at <c>Scripts/Generated/FlowLogType.&lt;Module&gt;.cs</c>, compiled into this assembly
    /// by the asmref beside it, and the project's <c>Default</c> under
    /// <c>Assets/Plugins/FlowIoC/Generated</c>. This part declares nothing; it is here so that the
    /// type exists in a project that has generated nothing yet, and so that
    /// <see cref="FlowLogChannels"/> has something to read.
    ///
    /// Every public <c>const string</c> on the class is a channel, and a <c>&lt;Name&gt;Color</c>
    /// or <c>&lt;Name&gt;Profile</c> field beside one says how its rows are drawn. The table reads
    /// them by reflection, which is what the attribute is for: a stripped player would otherwise be
    /// free to drop fields nothing references by name.
    /// </summary>
    [Preserve]
    public static partial class FlowLogType
    {
    }
}
