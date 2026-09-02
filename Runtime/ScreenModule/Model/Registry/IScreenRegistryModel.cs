using System;
using System.Collections.Generic;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Registry
{
    internal interface IScreenRegistryModel
    {
        void RegisterScreenManager(ScreenManagerVO manager);
        ScreenManagerVO GetScreenManager(int managerId);

        bool RegisterScreen(ScreenEntry entry);
        bool TryGetEntry(int managerId, Type viewType, out ScreenEntry entry);
        ScreenEntry GetEntry(int managerId, Type viewType);
        void RemoveEntry(ScreenEntry entry);

        List<ScreenEntry> GetAllEntries();
        List<ScreenEntry> GetManagerEntries(int managerId);
        List<ScreenEntry> GetTagEntries(ScreenTag tag);
        List<IScreenBody> GetAllLoadedScreens();
        List<IScreenBody> GetAllScreensAtManager(int managerId);

        void CopyDataFromConfig(ScreenVO screenData);
        void CopyDataFromConfig(ScreenVO screenData, ScreenCVO screen);
    }
}
