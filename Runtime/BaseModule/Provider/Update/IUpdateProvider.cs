using UnityEngine.Events;

namespace FlowIoC.BaseModule.Provider.Update
{
    public interface IUpdateProvider
    {
        void AddUpdate(UnityAction callback);
        void RemoveUpdate(UnityAction callback);
        
        void AddLateUpdate(UnityAction callback);
        void RemoveLateUpdate(UnityAction callback);
        
        void AddFixedUpdate(UnityAction callback);
        void RemoveFixedUpdate(UnityAction callback);
    }
}