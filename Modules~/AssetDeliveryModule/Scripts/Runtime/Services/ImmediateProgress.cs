using System;

namespace Modules.AssetDeliveryModule.Services
{
    /// <summary>
    /// Progress reported on the calling thread, at once. System.Progress posts through the
    /// synchronisation context, which in the Editor means a later tick - and a loading step
    /// reported a tick late is a bar that jumps.
    /// </summary>
    public class ImmediateProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public ImmediateProgress(Action<T> report) => _report = report;

        public void Report(T value) => _report?.Invoke(value);
    }
}
