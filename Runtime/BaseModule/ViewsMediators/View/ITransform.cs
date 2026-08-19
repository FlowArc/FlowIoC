using UnityEngine;

namespace FlowIoC.BaseModule.ViewsMediators.View
{
    public interface ITransform
    {
        Transform transform { get; }
        GameObject gameObject { get; }
    }
}