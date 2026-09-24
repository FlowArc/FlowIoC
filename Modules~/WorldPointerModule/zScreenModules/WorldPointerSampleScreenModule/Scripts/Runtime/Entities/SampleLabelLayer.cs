#if UNITY_EDITOR
using Modules.WorldPointerModule.Entities;
using Modules.WorldPointerModule.WorldPointerSampleScreenModule.Shared.Data.ValueObjects;

namespace Modules.WorldPointerModule.WorldPointerSampleScreenModule.Entities
{
    /// <summary>The ready display, closed over the sample's content. One line is all a game writes.</summary>
    public class SampleLabelLayer : WorldPointerLayer<WorldPointerSampleVO>
    {
    }
}
#endif
