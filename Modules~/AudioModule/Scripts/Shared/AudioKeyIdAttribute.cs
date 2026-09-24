using UnityEngine;

namespace Modules.AudioModule.Shared
{
    /// <summary>
    /// Marks a string field that holds an <see cref="AudioKey"/> id. The Inspector draws it as a
    /// list of the keys the project declares, so an id is picked rather than typed.
    /// </summary>
    public class AudioKeyIdAttribute : PropertyAttribute
    {
    }
}
