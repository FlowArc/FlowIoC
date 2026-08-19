using System;
using FlowIoC.BaseModule.Root;
using Object = UnityEngine.Object;

namespace FlowIoC.BaseModule.ViewsMediators.View.Data
{
    [Serializable]
    public class ViewInjectorData
    {
        public Object View;
        public bool AutoRegister;
        public bool UseBubbleUp = true;
        public bool InjectableView;
        public bool UseRootSelection = true;
        public RootBase SelectedRoot;
        public string RootName;
        public bool IsRegistered;
    }
}