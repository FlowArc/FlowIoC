using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.TempViews
{
    [RequireComponent(typeof(ViewInjector))]
    internal class TempView : MonoBehaviour, IView
    {
        public bool IsRegistered { get; set; }
        
        //@Actions
    }
}