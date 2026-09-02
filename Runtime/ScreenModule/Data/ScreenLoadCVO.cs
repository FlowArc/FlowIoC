namespace FlowIoC.ScreenModule.Data
{
    /// <summary>
    /// Where a screen's prefab comes from. A screen context declares this in its ScreenCVO; the
    /// load sub-services read it. There is no direct prefab reference because a context is plain
    /// C# and cannot serialize one, and the two remaining kinds cover a build.
    /// </summary>
    public readonly struct ScreenLoadCVO
    {
        public ScreenLoadType Kind { get; }

        /// <summary>The Addressables address, or the path under a Resources folder.</summary>
        public string Key { get; }

        private ScreenLoadCVO(ScreenLoadType kind, string key)
        {
            Kind = kind;
            Key = key;
        }

        public static ScreenLoadCVO Addressable(string address) => new(ScreenLoadType.Addressable, address);

        public static ScreenLoadCVO Resource(string path) => new(ScreenLoadType.Resource, path);

        /// <summary>A default value - a context that forgot to set Load - has no key and cannot be loaded.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Key);
    }
}
