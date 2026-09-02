using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.ScreenModule.Data;
using UnityEngine;

namespace FlowIoC.ScreenModule.ViewsMediators.Manager
{
    /// <summary>
    /// The scene side of the screen module: one canvas, its layers, and the id that Open's
    /// managerId names. Which screens it may open is not its business any more - every screen
    /// context registers itself with the service.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    internal class ScreenManager : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }
        public ScreenManagerVO ManagerData = new();
    }
}