using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.HapticModule.Enums;

namespace Modules.HapticModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a game's Command calls Play at the point where it decided something happened,
    /// which is where the choice of preset belongs. The steps a game binds instead of writing
    /// that Command sit inside, under <see cref="Commands"/>.
    /// </summary>
    public interface IHapticService
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
        /// and the flow reads from the Context: which preset, after which step.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Plays the preset given where the step is bound:
            /// <c>.ToSequence&lt;IHapticService.Commands.Play&gt;(HapticPreset.Success)</c>. A step
            /// whose preset is decided at runtime is a game Command injecting IHapticService; a
            /// previous step may also hand the preset on with Release(preset).
            /// </summary>
            public class Play : Command<HapticPreset>
            {
                [Inject] private IHapticService _haptics { get; set; }

                public override void Execute(HapticPreset preset) => _haptics.Play(preset);
            }

            /// <summary>
            /// Bound to the signal a settings toggle dispatches - a Signal&lt;bool&gt; - so the choice
            /// reaches the module without a Command of the game's own:
            /// <c>CommandBinder.Bind(_signals.HapticsToggled).ToSequence&lt;IHapticService.Commands.SetEnabled&gt;()</c>.
            /// </summary>
            public class SetEnabled : Command
            {
                [SignalParam] private bool _on { get; set; }

                [Inject] private IHapticService _haptics { get; set; }

                public override void Execute() => _haptics.SetEnabled(_on);
            }
        }
    }
}
