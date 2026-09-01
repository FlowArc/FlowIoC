using System;
using System.Collections.Generic;
using FlowIoC.ScreenModule.Layer;

namespace FlowIoC.ScreenModule.Data
{
    [Serializable]
    internal class ScreenManagerVO
    {
        public int ManagerID;
        public List<ScreenLayer> ScreenLayerList = new ();
        public Dictionary<Type, CD_Screen> ScreenConfigs = new();
    }
}