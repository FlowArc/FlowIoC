using System;
using Modules.LoadingModule.Shared.Enums;

namespace Modules.LoadingModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// One set as it stands right now, published whole on every change so a screen applies a
    /// snapshot rather than eight signals that only ever fire together. The Child fields are
    /// filled while a step that stands for another set is running, and are what the second bar
    /// draws.
    /// </summary>
    [Serializable]
    public class LoadingSetStatusRVO
    {
        public string Set;
        public LoadingPresentation Presentation;
        public LoadingSetState State;
        public float Progress;
        public string Message;
        public string Detail;
        public string FailedStep;

        public string ChildSet;
        public float ChildProgress;
        public string ChildMessage;
        public string ChildDetail;

        public bool HasChild => !string.IsNullOrEmpty(ChildSet);
    }
}
