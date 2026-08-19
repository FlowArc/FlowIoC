using FlowIoC.BaseModule.Controller.CommandGroup;

namespace FlowIoC.BaseModule.Controller
{
    public class CommandBody : ICommandBody
    {
        // [Inject] protected ICommandBinder _commandBinder { get; set; }
        public ICommandGroupResolver CommandGroupResolver { get; set; }
        public bool IsRetain { get; set; }
        public bool HasRetain { get; set; }

        public virtual void Retain()
        {
            IsRetain = true;
            HasRetain = true;
        }

        public virtual void Release(params object[] commandGroupData)
        {
            CommandGroupResolver.ReleaseCommand(this, commandGroupData);
        }
        public virtual void Stop()
        {
            CommandGroupResolver.StopCommand(this);
        }
        // public virtual void Jump<TCommandType>(params object[] commandGroupData)
        //     where TCommandType : ICommandBody
        // {
        //     CommandGroupResolver.JumpCommand<TCommandType>(this, commandGroupData);
        // }

        public virtual void Clean()
        {
            IsRetain = false;
            //HasRetain = false;
        }
    }
}