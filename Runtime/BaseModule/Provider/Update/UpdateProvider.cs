using System;
using FlowIoC.BaseModule.Attributes;
using UnityEngine;

namespace FlowIoC.BaseModule.Provider.Update
{
    [HideInModelViewer]
    public class UpdateProvider : MonoBehaviour, IUpdateProvider
    {
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