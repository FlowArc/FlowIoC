namespace FlowIoC.BaseModule.Constructables
{
    public interface IConstructable
    {
        bool IsPostConstructed { get; set; }
        bool IsDeConstructed { get; set; }
        void PostConstruct();
        void Deconstruct() { }
    }
}