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
        void RegisterScreenConfig(int managerId, Type type, CD_Screen config);
        void UnRegisterScreenConfig(int managerId, Type type, CD_Screen config);
        ScreenManagerVO GetScreenManager(int managerId);
        List<CD_Screen> GetAllRegisteredConfigs();
        List<CD_Screen> GetManagerConfigs(int managerId);
        List<CD_Screen> GetTagConfigs(ScreenTag tag);
        CD_Screen GetScreenConfig(int managerId, Type screenType);
        void ConfigToScreen(CD_Screen config, IScreenBody screen);
        bool IsScreenLoaded(CD_Screen config, out IScreenBody screen);
        List<IScreenBody> GetAllLoadedScreens();
        List<IScreenBody> GetAllScreensAtManager(int managerId);
        void CopyDataFromConfig(ScreenVO screenData);
        void CopyDataFromConfig(ScreenVO screenData, CD_Screen config);
        
    }
}