namespace Modules.ResourceFlyModule.Data.ValueObjects
{
    /// <summary>
    /// Where a flight leaves from, which resource it carries, and - optionally - the named look it
    /// flies with: fixed where the Fly step is bound.
    /// </summary>
    public readonly struct ResourceFlyRouteVO
    {
        public readonly string Source;
        public readonly string Key;
        public readonly string Look;

        public ResourceFlyRouteVO(string source, string key, string look = null)
        {
            Source = source;
            Key = key;
            Look = look;
        }

        public override string ToString() => string.IsNullOrEmpty(Look) ? $"{Key} from {Source}" : $"{Key} from {Source}, look {Look}";
    }
}