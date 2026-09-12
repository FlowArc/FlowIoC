using System;
using System.Collections.Generic;
using Modules.LoadingModule.Shared.Enums;
using UnityEngine;

namespace Modules.LoadingModule.Data.ValueObjects
{
    [Serializable]
    public class LoadingSetCVO
    {
        public string Key;
        public LoadingPresentation Presentation = LoadingPresentation.Fullscreen;

        [Tooltip("A running set that hears nothing for this long is reported once, naming the steps it is waiting for.")]
        public float StallWarningSeconds = 10f;

        public List<LoadingStepCVO> Steps = new();
    }
}
