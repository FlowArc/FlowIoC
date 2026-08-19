using System;

namespace FlowIoC.BaseModule.Root
{
    [Serializable]
    public struct SubContextData
    {
        public string ContextFullName;
        public string ContextName;

        public bool AutoSetup;
        public bool IsTest;
    }
}