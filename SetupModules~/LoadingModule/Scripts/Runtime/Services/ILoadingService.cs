using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace Modules.LoadingModule.Services
{
    /// <summary>
    /// Shows, waits for and times the game's loading; loads nothing itself. A set is begun by the
    /// module that owns the moment, its steps are reported by whoever does the work, and it ends
    /// when every step CD_LoadingSets lists for it has completed, skipped or failed.
    /// </summary>
    public interface ILoadingService
    {
        /// <summary>Opens the set's presentation. A Silent set needs no Begin; its first report begins it.</summary>
        void Begin(string set);

        /// <summary>The handle a Command reports through. The step's set comes from the config.</summary>
        ILoadingStep Report(string step);

        /// <summary>True when the set completed, false when it failed or the config does not know it.</summary>
        Task<bool> Await(string set);

        /// <summary>
        /// The steps a chain binds, the set named where the step is bound. They sit inside the
        /// interface so that the one name a game knows - the Service it injects - is also where
        /// its steps are found, and the boot reads from the Context top to bottom.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Begins a set: <c>.ToSequence&lt;ILoadingService.Commands.Begin&gt;("Boot")</c>.
            /// Synchronous - the screen opens on its own signal, and the chain carries on to the
            /// steps it will report.
            /// </summary>
            public class Begin : Command<string>
            {
                [Inject] private ILoadingService _loadingService { get; set; }

                public override void Execute(string set) => _loadingService.Begin(set);
            }

            /// <summary>
            /// Waits for a set: <c>.ToSequence&lt;ILoadingService.Commands.Await&gt;("Boot")</c>. The
            /// chain's own steps are already waited on by the group; this is for the steps other
            /// modules report on their own, and it is what holds the chain behind a failed boot.
            /// Three ways out, and every one resolves the retain: completed releases, failed
            /// stops, and a throw stops.
            /// </summary>
            public class Await : Command<string>
            {
                [Inject] private ILoadingService _loadingService { get; set; }

                public override async void Execute(string set)
                {
                    Retain();

                    try
                    {
                        bool completed = await _loadingService.Await(set);

                        if (!completed)
                        {
                            // The failure itself was logged where it happened; this line only says the chain stopped.
                            FlowLogger.Log($"ILoadingService.Commands.Await - '{set}' did not complete, so the sequence stops here.");
                            Stop();
                            return;
                        }

                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"ILoadingService.Commands.Await threw while waiting for '{set}': {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
