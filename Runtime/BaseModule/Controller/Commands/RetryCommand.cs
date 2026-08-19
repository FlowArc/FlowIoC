using System.Threading.Tasks;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Controller.Commands
{
    public abstract class RetryCommand : Command <int,float>
    {
        protected int _retryCount;
        protected int _retryLimit;
        protected float _retryDelay;
        
        public override void Execute(int retryLimit, float delay)
        {
            Retain();
            _retryCount = 0;
            _retryLimit =  retryLimit;
            _retryDelay = delay;
        }

        protected abstract void Try();
        
        protected async void TryFailed()
        {
            FlowLogger.Log(SystemLogType.Command, "[RetryCommand.TryFailed] failed");
            if (_retryCount == _retryLimit)
                RetryFailLimitReached();
            else if (_retryDelay <= 0)
                Retry();
            else
                await WaitAndRetry();
        }
        private async Task WaitAndRetry()
        {
            await Task.Delay((int)(_retryDelay * 1000));
            Retry();
        }

        protected virtual void Retry()
        {
            _retryCount++;
            FlowLogger.Log(SystemLogType.Command, $"[RetryCommand.Retry] Count:{_retryCount}");
            Try();
        }

        protected virtual void RetryFailLimitReached()
        {
            FlowLogger.Log(SystemLogType.Command, "[RetryCommand.Stop] Retry Fail LimitReached");
            Stop();
        }
    }
}