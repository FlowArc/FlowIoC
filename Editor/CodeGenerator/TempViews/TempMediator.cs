using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;

namespace FlowIoC.Editor.CodeGenerator.TempViews
{
    internal class TempMediator : IMediator
    {
        [Inject] private TempView _view { get; set; }
        
        public void OnRegister()
        {
            //@Register
        }

        public void OnRemove()
        {
            //@Remove
        }
        
        //@Methods
    }
}