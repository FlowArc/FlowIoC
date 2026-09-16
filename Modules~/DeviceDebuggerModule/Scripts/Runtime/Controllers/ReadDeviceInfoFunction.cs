using System.Collections.Generic;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Controllers
{
    /// <summary>The Info tab's rows: what the build is and what it runs on, read once at initialize.</summary>
    internal class ReadDeviceInfoFunction : FunctionReturn<List<InfoRowVO>>
    {
        public override List<InfoRowVO> Execute()
        {
            Rect safeArea = Screen.safeArea;

            return new List<InfoRowVO>
            {
                new("Product", Application.productName),
                new("Version", Application.version),
                new("Unity", Application.unityVersion),
                new("Build", Debug.isDebugBuild ? "Development" : "Release"),
                new("Platform", Application.platform.ToString()),
                new("Install mode", Application.installMode.ToString()),
                new("Device", SystemInfo.deviceModel),
                new("OS", SystemInfo.operatingSystem),
                new("CPU", SystemInfo.processorType + " x" + SystemInfo.processorCount),
                new("RAM", SystemInfo.systemMemorySize + " MB"),
                new("GPU", SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ", " + SystemInfo.graphicsMemorySize + " MB)"),
                new("Screen", Screen.width + " x " + Screen.height + " @ " + Screen.dpi.ToString("0") + " dpi"),
                new("Safe area", safeArea.width.ToString("0") + " x " + safeArea.height.ToString("0") + " at " + safeArea.x.ToString("0") + ", " + safeArea.y.ToString("0")),
                new("Language", Application.systemLanguage.ToString()),
                new("Internet", Application.internetReachability.ToString()),
                new("Data path", Application.persistentDataPath)
            };
        }
    }
}
