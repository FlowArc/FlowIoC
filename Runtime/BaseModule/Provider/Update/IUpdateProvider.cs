using System;

namespace FlowIoC.BaseModule.Provider.Update
{
    public interface IUpdateProvider
    {
        void AddUpdate(Action callback);
        void RemoveUpdate(Action callback);
        
        void AddLateUpdate(Action callback);
        void RemoveLateUpdate(Action callback);
        
        void AddFixedUpdate(Action callback);
        void RemoveFixedUpdate(Action callback);
    }
}