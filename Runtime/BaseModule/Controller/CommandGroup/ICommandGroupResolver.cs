using System;
using FlowIoC.BaseModule.Controller.Binders;

namespace FlowIoC.BaseModule.Controller.CommandGroup
{
    public interface ICommandGroupResolver
    {
        Action<ICommandGroupResolver> GroupExecutionFinished { get; set; }

        void Initialize(ICommandBinding commandBinding, CommandBinder commandBinder, params object[] signalParameters);
        void ReleaseCommand(ICommandBody command, params object[] commandParameters);
        void StopCommand(ICommandBody commandBody);

        // void JumpCommand<TCommandType>(ICommandBody command, params object[] commandParameters)
        //     where TCommandType : ICommandBody;

        void Dispose();
    }
}