using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace FlowIoC.Editor.CodeGenerator.TempModels
{
    internal class TempModel : ITempModel, IConstructable
    {
        //@Injectables

        public bool IsPostConstructed { get; set; }
        public bool IsDeConstructed { get; set; }
        public void PostConstruct()
        {
        }
    }
}