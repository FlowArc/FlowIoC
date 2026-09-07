using FlowIoC.BaseModule.Controller.CommandGroup;

namespace FlowIoC.BaseModule.Controller
{
    public interface ICommandBody
    {
        ICommandGroupResolver CommandGroupResolver { get; }
        bool IsRetain { get; }
        bool HasRetain { get; }

        void Retain();
        void Release(params object[] commandGroupData);
        void Stop();
        void Clean();
    }
}