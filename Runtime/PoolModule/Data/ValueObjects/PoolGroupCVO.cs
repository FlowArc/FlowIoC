using FlowIoC.PoolModule.Data.UnityObjects;

namespace FlowIoC.PoolModule.Data.ValueObjects
{
    [System.Serializable]
    public class PoolGroupCVO
    {
        public CD_PoolGroup Group;
        public bool AutoInitialize;
        public bool GroupSpecificPools;
    }
}