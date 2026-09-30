namespace FlowIoC.PoolModule.Data.ValueObjects
{
    /// <summary>
    /// One pool key, held whole by PoolConfigModel: the item's entry in its group's asset, and the
    /// group that registered it. It holds a config side and a registration side together, which is
    /// why it is a plain VO.
    /// </summary>
    internal class PoolItemVO
    {
        public PoolItemBaseCVO Config;

        /// <summary>The key of the group that registered this pool key last.</summary>
        public string Group;
    }
}
