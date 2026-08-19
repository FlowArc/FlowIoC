using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Config
{
    internal interface IScreenConfigModel
    {
        void RegisterScreenManager(ScreenManagerVO manager, IContext registeredContext);
        IContext GetManagerRegisteredContext(int managerId);
        void RegisterScreenConfig(int managerId, Type type, ScreenConfig config);
        void UnRegisterScreenConfig(int managerId, Type type, ScreenConfig config);
        ScreenManagerVO GetScreenManager(int managerId);
        List<ScreenConfig> GetAllRegisteredConfigs();
        List<ScreenConfig> GetManagerConfigs(int managerId);
        List<ScreenConfig> GetTagConfigs(ScreenTag tag);
        ScreenConfig GetScreenConfig(int managerId, Type screenType);
        void ConfigToScreen(ScreenConfig config, IScreenBody screen);
        bool IsScreenLoaded(ScreenConfig config, out IScreenBody screen);
        List<IScreenBody> GetAllLoadedScreens();
        List<IScreenBody> GetAllScreensAtManager(int managerId);
        void CopyDataFromConfig(ScreenVO screenData);
        void CopyDataFromConfig(ScreenVO screenData, ScreenConfig config);
        
    }
}