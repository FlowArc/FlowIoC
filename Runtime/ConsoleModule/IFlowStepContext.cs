namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// A step of a flow whose lines are hidden, able to say where in that flow it is. A warning or
    /// an error written while it runs carries the answer, so a tick that writes nothing frame after
    /// frame still says, on the one line that matters, which step failed and which ran before it.
    /// </summary>
    public interface IFlowStepContext
    {
        /// <summary>Where the flow is: its signal, the step running and the steps before it.</summary>
        string DescribeStep();
    }
}
