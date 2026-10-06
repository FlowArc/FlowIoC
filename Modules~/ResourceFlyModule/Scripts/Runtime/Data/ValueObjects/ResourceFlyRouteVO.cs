namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>Where a flight leaves from and which resource it carries - fixed where the Fly step is bound.</summary>
    public readonly struct ResourceFlyRouteVO
    {
        public readonly string Source;
        public readonly string Key;

        public ResourceFlyRouteVO(string source, string key)
        {
            Source = source;
            Key = key;
        }

        public override string ToString() => $"{Key} from {Source}";
    }
}
