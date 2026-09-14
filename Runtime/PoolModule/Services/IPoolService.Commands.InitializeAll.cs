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
            /// Fills every configured pool group and holds the sequence until the last fill is
            /// done. A group whose fill fails is reported where it failed; the step stops the
            /// sequence, so nothing runs on an empty pool.
            /// </summary>
            public class InitializeAll : Command
            {
                [Inject] private IPoolService _poolService { get; set; }

                public override async void Execute()
                {
                    Retain();

                    try
                    {
                        await _poolService.InitializeAllAsync();
                        Release();
                    }
                    catch (Exception exception)
                    {
                        FlowLogger.LogError($"IPoolService.Commands.InitializeAll threw while filling the pools: {exception}");
                        Stop();
                    }
                }
            }
        }
    }
}
