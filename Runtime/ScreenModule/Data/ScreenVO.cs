using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Data
{
    [ShowInModelViewer]
    [Serializable]
    public class ScreenVO
    {
        public Type ScreenType { get; set; }
        public ScreenState State { get; set; } = new();
        public int ManagerId { get; set; } = 0;
        public int LayerIndex { get; set; }
        public bool ForceOpenAtFullLayer { get; set; }
        public bool ForceOpenAtFullLayerWithHideAnim { get; set; }
        public bool ForceOpenAtDuplication { get; set; }
        public bool ForceOpenForDuplicationWithHideAnim { get; set; }
        
        public ScreenTag Tag { get; set; }
        public object[] Parameters { get; set; }
        public bool HasShowAnimation { get; set; }
        public bool HasHideAnimation { get; set; }
        public bool AddToHistory { get; set; }
    }
}