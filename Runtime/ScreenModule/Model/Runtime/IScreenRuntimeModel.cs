using System;
using System.Collections.Generic;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Runtime
{
    internal interface IScreenRuntimeModel
    {
        void AddToPassivePool(IScreenBody screenBody);
        bool GetScreen<T>(int managerId, out T screen) where T : IScreenBody;

        /// <summary>
        /// The same checkout by type, for a caller that holds the screen's type as a value - the
        /// builder, which was opened with a type parameter it no longer has at Show.
        /// </summary>
        bool GetScreen(int managerId, Type screenType, out IScreenBody screen);

        void AddToActivePools(IScreenBody screenBody);
        void RemoveFromActivePools(IScreenBody screenBody);
        void RemoveFromPassivePool(IScreenBody screenBody);
        bool IsLayerFull(int layerIndex, int managerId, out IScreenBody screenBody);
        bool IsScreenActive(Type screenType, int managerId, out IScreenBody screenBody);
        List<IScreenBody> GetAllActiveScreens();
        bool GetActiveManagerScreens(int managerId, out List<IScreenBody> list);
        bool GetActiveTagScreens(ScreenTag tag, int managerId, out List<IScreenBody> list);
    }
}
