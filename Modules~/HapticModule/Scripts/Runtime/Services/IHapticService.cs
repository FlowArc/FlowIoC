using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a game's Command calls Play at the point where it decided something happened,
    /// which is where the choice of preset belongs. The steps a game binds instead of writing
    /// that Command sit under <see cref="Commands"/>, one file each beside this one.
    /// </summary>
    public partial interface IHapticService
    {
        /// <summary>
        /// Plays one preset. Nothing happens while haptics are off, for None, or on a platform
        /// with nothing to vibrate; none of those is an error.
        /// </summary>
        void Play(HapticPreset preset);

        /// <summary>The stored choice. True until a game turns it off.</summary>
        bool IsEnabled();

        /// <summary>Stores the choice and applies it. Turning haptics off stops a vibration in progress.</summary>
        void SetEnabled(bool on);

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found,
        /// and the flow reads from the Context: which preset, after which step. Each step is a
        /// file of its own, <c>IHapticService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
