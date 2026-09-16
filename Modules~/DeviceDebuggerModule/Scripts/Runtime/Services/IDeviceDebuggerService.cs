using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a settings screen's hidden button, a cheat gesture of the game's own, anything
    /// that wants the panel open calls Show. What a game exposes on the panel is not declared
    /// here - a [DebugOption] on a signal field or a shipped step is found by the module itself.
    /// The steps a game binds instead of writing a Command sit under <see cref="Commands"/>.
    /// </summary>
    public partial interface IDeviceDebuggerService
    {
        /// <summary>False in a release build: the Root stays, the panel does not exist, every call is a no-op.</summary>
        bool IsAvailable { get; }

        bool IsOpen { get; }

        /// <summary>Opens on the tab that was open before, or the Console when an error waits unread.</summary>
        void Show();

        void Show(DebugTab tab);

        void Hide();

        void Toggle();

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found.
        /// Each step is a file of its own, <c>IDeviceDebuggerService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
