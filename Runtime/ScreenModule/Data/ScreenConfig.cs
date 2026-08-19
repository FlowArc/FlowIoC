using System;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FlowIoC.ScreenModule.Data
{
    [CreateAssetMenu(fileName = "ScreenConfig", menuName = "FlowIoC/Screen/Screen Config")]
    public class ScreenConfig : ScriptableObject
    {
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

        public GameObject DirectPrefab
        {
            get => _directPrefab;
            set => _directPrefab = value;
        }

        public ScreenTag Tag
        {
            get => _screenTag;
            set => _screenTag = value;
        }

        public string ResourcePath => _resourcePath;

        public string AddressableKey
        {
            get => _addressableKey;
            set => _addressableKey = value;
        }

        public bool HasShowAnimation => _hasShowAnimation;
        public bool HasHideAnimation => _hasHideAnimation;

        public string ViewTypeName => _viewTypeName;
        public string MediatorTypeName => _mediatorTypeName;

        public Type ViewType => string.IsNullOrEmpty(_viewTypeName) ? null : Type.GetType(_viewTypeName);
        public Type MediatorType => string.IsNullOrEmpty(_mediatorTypeName) ? null : Type.GetType(_mediatorTypeName);

#if UNITY_EDITOR
        private void OnValidate()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_defaultLayer < 0)
            {
                Debug.LogError($"Default Layer must be greater than or equal to 0 in {name}!");
            }

            switch (_loadType)
            {
                case ScreenLoadType.DirectPrefab:
                    if (_directPrefab == null)
                    {
                        Debug.LogError($"Direct Prefab is required for DirectPrefab load type in {name}!");
                    }
                    else if (_directPrefab.GetComponent<IScreenBody>() == null)
                    {
                        Debug.LogError($"Prefab must have an IScreenBody component in {name}!");
                    }

                    break;

                case ScreenLoadType.Resource:
                    if (string.IsNullOrEmpty(_resourcePath))
                    {
                        Debug.LogError($"Resource Path is required for Resource load type in {name}!");
                    }

                    break;

                case ScreenLoadType.Addressable:
                    if (string.IsNullOrEmpty(_addressableKey))
                    {
                        Debug.LogError($"Addressable Key is required for Addressable load type in {name}!");
                    }

                    break;
            }
        }
#endif
        
    }
}