namespace FlowIoC.BaseModule.Constructables
{
    public interface IConstructable
    {
        bool IsPostConstructed { get; set; }
        bool IsDeconstructed { get; set; }
        void PostConstruct();
        void Deconstruct() { }
    }
}