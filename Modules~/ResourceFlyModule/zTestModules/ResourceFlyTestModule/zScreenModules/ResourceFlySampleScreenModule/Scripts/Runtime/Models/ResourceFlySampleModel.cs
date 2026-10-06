#if UNITY_EDITOR
using System.Collections.Generic;

namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Models
{
    /// <summary>The sample's saved values, in place of a game's profile. Starts every Play at zero.</summary>
    internal class ResourceFlySampleModel : IResourceFlySampleModel
    {
        private readonly Dictionary<string, int> _saved = new();

        public int Grant(string key, int amount)
        {
            _saved.TryGetValue(key, out int saved);
            saved += amount;
            _saved[key] = saved;
            return saved;
        }
    }
}
#endif
