using System.Collections.Generic;
using Modules.AnalyticsModule.Data.ValueObjects;

namespace Modules.AnalyticsModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a game's Command calls Log at the point where it decided something happened,
    /// which is where the event belongs. The steps a game binds instead of writing that Command
    /// sit under <see cref="Commands"/>, one file each beside this one. Plug and Unplug are for
    /// a provider's hosted context, never for a game's Command.
    /// </summary>
    public partial interface IAnalyticsService
    {
        /// <summary>Which providers are plugged and where each one is; what a debug label reads.</summary>
        IReadOnlyList<ProviderSlotVO> Slots { get; }

        void Log(AnalyticsEventVO analyticsEvent);

        void SetUserProperty(string name, string value);

        void SetUserId(string userId);

        /// <summary>Handed on to every provider as given; the module resolves nothing.</summary>
        void SetConsent(AnalyticsConsentVO consent);

        /// <summary>Called by a provider's hosted context in its Setup. A provider plugged after Launch is initialized at once.</summary>
        void Plug(IAnalyticsProvider provider);

        void Unplug(IAnalyticsProvider provider);

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found,
        /// and the flow reads from the Context: which event, after which step. Each step is a
        /// file of its own, <c>IAnalyticsService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
