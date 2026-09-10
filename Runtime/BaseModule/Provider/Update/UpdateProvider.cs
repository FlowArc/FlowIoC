using System;
using FlowIoC.BaseModule.Attributes;
using UnityEngine;

namespace FlowIoC.BaseModule.Provider.Update
{
    /// <summary>
    /// The one place a module hooks a frame. Ordered after the cameras: CinemachineBrain moves
    /// the camera in a LateUpdate of its own at order 0, and a callback here that ran before it
    /// saw last frame's camera - a pointer placed over the world lagged visibly while the camera
    /// moved, and nothing said why.
    /// </summary>
    [HideInModelViewer]
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class UpdateProvider : MonoBehaviour, IUpdateProvider
    {
        /// <summary>
        /// After Cinemachine (0) and after anything a game orders by hand with a modest number, so
        /// that a LateUpdate callback sees the camera and the animation where this frame left them.
        /// A module that must run before the camera has AddUpdate for that.
        /// </summary>
        public const int EXECUTION_ORDER = 1000;

        private event Action OnUpdate;
        private event Action OnLateUpdate;
        private event Action OnFixedUpdate;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        #region Update

        public void AddUpdate(Action callback)
        {
            OnUpdate += callback;
        }

        public void RemoveUpdate(Action callback)
        {
            OnUpdate -= callback;
        }

        #endregion

        #region LateUpdate

        public void AddLateUpdate(Action callback)
        {
            OnLateUpdate += callback;
        }

        public void RemoveLateUpdate(Action callback)
        {
            OnLateUpdate -= callback;
        }

        #endregion

        #region FixedUpdate

        public void AddFixedUpdate(Action callback)
        {
            OnFixedUpdate += callback;
        }

        public void RemoveFixedUpdate(Action callback)
        {
            OnFixedUpdate -= callback;
        }

        #endregion

        #region UnityCallback

        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        #endregion
    }
}