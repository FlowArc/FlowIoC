namespace FlowIoC.BaseModule.Function.Provider
{
    public interface IFunctionDataContainer
    {
        IFunctionDataContainer AddParams(params object[] executeParameters);
        TReturnType ExecuteAndGetResult<TReturnType>();
        void Execute();
    }
}