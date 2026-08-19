using UnityEngine;

namespace FlowIoC.ScreenModule.Layer
{
    internal class ScreenLayer : MonoBehaviour, IScreenLayer
    {
        [SerializeField] private bool _isSafeAreaExists = true;
        public bool IsSafeAreaExists => _isSafeAreaExists;

    }
}