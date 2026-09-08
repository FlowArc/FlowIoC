using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Controller.Commands
{
    /// <summary>
    /// A command that tries something and, when told it failed, tries again up to a limit with a
    /// pause in between. The pause runs on the framework's coroutine provider in real time: it
    /// used to be a Task.Delay on an async void, which swallowed whatever the retry threw, could
    /// not be stopped with the scene, and never fired on WebGL.
    /// </summary>
    public abstract class RetryCommand : Command<int, float>
    {
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }

        protected int _retryCount;
        protected int _retryLimit;
        protected float _retryDelay;

        public override void Execute(int retryLimit, float delay)
        {
            Retain();
            _retryCount = 0;
            _retryLimit = retryLimit;
            _retryDelay = delay;
        }

        protected abstract void Try();

        protected void TryFailed()
        {
            FlowLogger.Log(SystemLogType.Command, "[RetryCommand.TryFailed]");

            if (_retryCount == _retryLimit)
                RetryFailLimitReached();
            else if (_retryDelay <= 0)
                Retry();
            else
                _coroutineProvider.WaitForSecondsRealTime(_retryDelay, Retry);
        }

        protected virtual void Retry()
        {
            _retryCount++;
            FlowLogger.Log(SystemLogType.Command, "[RetryCommand.Retry][count(", _retryCount.ToString(), ")]");
            Try();
        }

        protected virtual void RetryFailLimitReached()
        {
            FlowLogger.Log(SystemLogType.Command, "[RetryCommand.RetryFailLimitReached]");
            Stop();
        }
    }
}
