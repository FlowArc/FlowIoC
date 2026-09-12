using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FlowIoC.AssetModule.Gateway
{
    /// <summary>
    /// An AsyncOperationHandle as the service reads it. Every member answers something sensible
    /// for a handle that was released: done, not succeeded, no result, a completed task - so the
    /// service never has to ask IsValid before looking.
    /// </summary>
    internal sealed class AddressableHandle : IAssetHandle
    {
        private readonly AsyncOperationHandle _handle;

        public AddressableHandle(AsyncOperationHandle handle)
        {
            _handle = handle;
        }

        public bool IsValid => _handle.IsValid();

        public bool IsDone => !_handle.IsValid() || _handle.IsDone;

        public bool Succeeded => _handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded;

        public float PercentComplete => _handle.IsValid() ? _handle.PercentComplete : 1f;

        public object Result => _handle.IsValid() ? _handle.Result : null;

        public Task Task => _handle.IsValid() ? _handle.Task : System.Threading.Tasks.Task.CompletedTask;

        public object WaitForCompletion() => _handle.IsValid() ? _handle.WaitForCompletion() : null;

        public void Release()
        {
            if (_handle.IsValid())
                Addressables.Release(_handle);
        }
    }
}
