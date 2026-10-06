#if UNITY_EDITOR
namespace Modules.ResourceFlyModule.ResourceFlyTestModule.ResourceFlySampleScreenModule.Models
{
    internal interface IResourceFlySampleModel
    {
        /// <summary>Adds the grant to the key's saved value and returns the new value.</summary>
        int Grant(string key, int amount);
    }
}
#endif
