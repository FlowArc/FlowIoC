namespace FlowIoC.BaseModule.Function.Provider
{
    public interface IFunctionDataContainer
    {
        IFunctionDataContainer AddParams(params object[] executeParameters);
        TReturnType SetReturn<TReturnType>();
        void SetVoid();
    }
}