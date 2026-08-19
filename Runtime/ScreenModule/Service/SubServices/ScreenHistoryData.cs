namespace FlowIoC.ScreenModule.Service.SubServices
{
    internal class ScreenHistoryData
    {
        public int ManagerIndex { get; }
        public System.Type ScreenType { get; }
        public int LayerIndex { get; }

        public ScreenHistoryData(int managerIndex, System.Type screenType, int layerIndex)
        {
            ManagerIndex = managerIndex;
            ScreenType = screenType;
            LayerIndex = layerIndex;
        }
    }
} 