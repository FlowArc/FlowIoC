using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowIoC.AssetModule.Service
{
    /// <summary>
    /// <i><b>&gt;FlowIoC&lt;</b></i> — merkezî Addressable asset yönetimi.
    /// Asset yükle/kullan/release et; runtime grupları ile toplu yükle/release/gruba-ekle.
    /// LOAD-ONCE: bir asset yalnız bir kez yüklenir; tekrar çağrılar cache'i verir veya
    /// devam eden yüklemeyi bekler. Instantiate ETMEZ — prefab gerekiyorsa çağıran kendi Instantiate eder.
    /// </summary>
    public interface IAssetService
    {
        Task<T> LoadAssetAsync<T>(object key, string groupId = null);
        T LoadAsset<T>(object key, string groupId = null);
        bool TryGetAsset<T>(object key, out T asset);
        void Release(object key, string groupId = null);

        Task LoadGroupByLabelAsync<T>(string label, string groupId = null);
        Task LoadAssetsAsync<T>(string groupId, IEnumerable<object> keys);
        void AddToGroup(string groupId, object key);
        void ReleaseGroup(string groupId);
        bool IsGroupLoaded(string groupId);
        IReadOnlyCollection<string> GetGroupKeys(string groupId);
    }
}
