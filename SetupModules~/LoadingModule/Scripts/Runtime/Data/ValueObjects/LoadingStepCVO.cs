using System;
using UnityEngine;

namespace Modules.LoadingModule.Data.ValueObjects
{
    [Serializable]
    public class LoadingStepCVO
    {
        [Tooltip("Unique across the whole project. The Command that does the work names only this.")]
        public string Key;

        [Tooltip("This step's share of the set's bar, relative to the other steps.")]
        public float Weight = 1f;

        [Tooltip("Shown while this step is the most recent one running.")]
        public string Message;

        [Tooltip("Optional. This step stands for another set: its progress is that set's, and it completes when that set completes.")]
        public string ChildSet;

        public bool HasChild => !string.IsNullOrEmpty(ChildSet);
    }
}
