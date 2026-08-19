using System;
using UnityEngine;

namespace FlowIoC.PoolModule.Entities
{
    public class PoolableItem : MonoBehaviour, IPoolableItem
    {
        public string ItemKey { get; set; }
        public Action<IPoolableItem> ReturnToPoolAction { get; set; }
        public virtual void SetActive(bool value = true) => gameObject.SetActive(value);
        public virtual void Dismiss() => ReturnToPoolAction?.Invoke(this);
        public virtual void OnInitialized() { }
        public virtual void OnGetFromPool() { }
        public virtual void OnReturnToPool() { }
    }
}