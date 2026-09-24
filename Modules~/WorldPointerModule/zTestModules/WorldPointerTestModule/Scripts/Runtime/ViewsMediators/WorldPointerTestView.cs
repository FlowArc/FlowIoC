#if UNITY_EDITOR
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;

namespace Modules.WorldPointerModule.WorldPointerTestModule.ViewsMediators
{
    /// <summary>
    /// The world of the sample, nothing else: the camera pivot turns and the cubes stand still.
    /// It draws no UI - the pointers are the sample screen's, the buttons the controls screen's.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class WorldPointerTestView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }

        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private float _orbitDegreesPerSecond = 20f;
        [SerializeField] private Transform[] _targets;

        public Transform[] Targets => _targets;

        private void Update()
        {
            if (_cameraPivot != null)
                _cameraPivot.Rotate(0f, _orbitDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
#endif