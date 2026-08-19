using FlowIoC.BaseModule.Controller.CommandGroup;

namespace FlowIoC.BaseModule.Controller
{
    public interface ICommandBody
    {
        ICommandGroupResolver CommandGroupResolver { get; set; }
        bool IsRetain { get; set; }
        bool HasRetain { get; set; }

        void Retain();
        void Release(params object[] commandGroupData);
        void Stop();
        void Clean();
    }
}