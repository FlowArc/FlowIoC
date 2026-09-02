#if UNITY_EDITOR

using FlowIoC.ScreenModule.Enums;
using UnityEngine;

namespace FlowIoC.ScreenModule.Data
{
    /// <summary>
    /// The screen config asset FlowIoC used before a screen declared itself in its context. It
    /// stays, editor-only and read-only, so the assets a project still has load long enough for
    /// ScreenConfigMigrator to turn them into contexts. Nothing at runtime knows this type, and
    /// nothing creates new ones. It goes away one release after the migrator does its work.
    /// </summary>
    public class CD_Screen : ScriptableObject
    {
        // DirectPrefab was the third ScreenLoadType. The enum no longer has it, but an asset that
        // was set to it still holds the raw 2, and the migrator has to recognise that asset as one
        // it cannot finish by itself.
        private const int LegacyDirectPrefab = 2;

        [SerializeField] private int _defaultLayer;
        [SerializeField] private ScreenLoadType _loadType;
        [SerializeField] private GameObject _directPrefab;
        [SerializeField] private ScreenTag _screenTag;
        [SerializeField] private string _resourcePath;
        [SerializeField] private string _addressableKey;
        [SerializeField] private bool _hasShowAnimation;
        [SerializeField] private bool _hasHideAnimation;
        [SerializeField] private string _viewTypeName;
        [SerializeField] private string _mediatorTypeName;

        public int DefaultLayer => _defaultLayer;
        public ScreenLoadType LoadType => _loadType;
        public bool WasDirectPrefab => (int) _loadType == LegacyDirectPrefab;
        public ScreenTag Tag => _screenTag;
        public string ResourcePath => _resourcePath;
        public string AddressableKey => _addressableKey;
        public bool HasShowAnimation => _hasShowAnimation;
        public bool HasHideAnimation => _hasHideAnimation;
        public string ViewTypeName => _viewTypeName;
        public string MediatorTypeName => _mediatorTypeName;

        /// <summary>The prefab a DirectPrefab config pointed at; only the migrator's report reads it.</summary>
        public GameObject DirectPrefab => _directPrefab;
    }
}

#endif
