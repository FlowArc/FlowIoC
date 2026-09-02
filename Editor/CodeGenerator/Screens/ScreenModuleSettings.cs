#if UNITY_EDITOR

using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.Editor.CodeGenerator.Screens
{
    /// <summary>
    /// What Create Module asks about a screen, written straight into the generated context's
    /// ScreenCVO. This used to be serialized into a CD_Screen asset; the context is the only
    /// declaration now, so these are inputs to a template rather than data that lives on.
    /// </summary>
    internal class ScreenModuleSettings
    {
        public int ManagerId;
        public int Layer;
        public ScreenTag Tag = ScreenTag.Default;
        public ScreenLoadType LoadType = ScreenLoadType.Addressable;

        /// <summary>The Addressables address. The generator sets it to the module name, which is also the prefab's address.</summary>
        public string AddressableKey;

        public string ResourcePath;
        public bool HasShowAnimation;
        public bool HasHideAnimation;
    }
}

#endif
