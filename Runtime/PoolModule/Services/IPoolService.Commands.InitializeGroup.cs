using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;

namespace FlowIoC.PoolModule.Services
{
    public partial interface IPoolService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Fills one pool group, the group named where the step is bound -
            /// <c>.ToSequence&lt;IPoolService.Commands.InitializeGroup&gt;("Match")</c> - and holds the
            /// sequence until it is filled. A group the config does not know is a warning and an
            /// empty fill, so the sequence carries on; a fill that throws stops it.
            /// </summary>
            public class InitializeGroup : Command<string>
            {
                [Inject] private IPoolService _poolService { get; set; }

                public override async void Execute(string group)
                {
                    Retain();

                    try
                    {
                        await _poolService.InitializeGroupAsync(group);
                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IPoolService.Commands.InitializeGroup threw while filling '{group}': {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
