using System.Threading.Tasks;

namespace FlowIoC.AssetModule.Gateway
{
    /// <summary>
    /// One Addressables operation, as the service sees it. The real one wraps an
    /// AsyncOperationHandle; the tests' one completes when the test says. Nothing outside the
    /// module holds one.
    /// </summary>
    internal interface IAssetHandle
    {
        bool IsValid { get; }

        bool IsDone { get; }

        /// <summary>Meaningful once IsDone.</summary>
        bool Succeeded { get; }

        float PercentComplete { get; }

        object Result { get; }

        Task Task { get; }

        object WaitForCompletion();

        void Release();
    }
}
