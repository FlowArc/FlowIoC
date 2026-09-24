using System.Collections.Generic;
using Modules.WorldPointerModule.Data.ValueObjects;

namespace Modules.WorldPointerModule.Models
{
    /// <summary>The module's own status asset. Bound locally: only the service crosses.</summary>
    internal interface IWorldPointerModel
    {
        /// <summary>Replaces every row of RD_WorldPointer with these. Nothing happens without the asset.</summary>
        void Publish(List<WorldPointerChannelRVO> channels);
    }
}
