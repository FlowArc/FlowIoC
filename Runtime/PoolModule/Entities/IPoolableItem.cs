using System;
using UnityEngine;

namespace FlowIoC.PoolModule.Entities
{
    public interface IPoolableItem
    {
        string ItemKey { get; set; }
        Action<IPoolableItem> ReturnToPoolAction { get; set; }
        Transform transform { get; }
        void SetActive(bool value = true);
        void OnInitialized();
        void OnGetFromPool();
        void OnReturnToPool();
        /// <summary>
        /// use => ReturnToPoolAction?.Invoke(this);
        /// </summary>
        void Dismiss();
    }
}
