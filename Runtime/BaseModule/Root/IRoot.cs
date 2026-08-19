using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;

namespace FlowIoC.BaseModule.Root
{
    public interface IRoot
    {
        string Name { get; }
        void StartContext(bool forceToStart = false);
        void InitializeSubContexts();
        IContext GetContext();
        Stack<IContext> GetSubContexts();
        Stack<IContext> GetAllContexts();
        void Setup(bool forceToSetup = false);
        void Launch(bool forceToLaunch = false);
        
    }
}