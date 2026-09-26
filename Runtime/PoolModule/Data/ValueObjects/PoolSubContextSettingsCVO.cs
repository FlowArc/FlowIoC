using System;
using FlowIoC.BaseModule.Root;
using UnityEngine.Rendering;

namespace FlowIoC.PoolModule.Data.ValueObjects
{
    /// <summary>
    /// The pool groups a Root's module uses, written on the PoolSubContext entry of that Root. The
    /// module that spawns the objects is the one that knows them, so its Root carries them - the
    /// pool service's own Root is never edited.
    /// </summary>
    [Serializable]
    public sealed class PoolSubContextSettingsCVO : SubContextSettingsCVO
    {
        /// <summary>Group key to group. The key is what InitializeGroup and Return.Group take.</summary>
        public SerializedDictionary<string, PoolGroupCVO> Groups = new();

        /// <summary>
        /// Tears the groups down when this Root goes - a Root in a scene loaded on top of another,
        /// whose pools should leave with it. Off, the groups outlive the Root that registered them.
        /// </summary>
        public bool UnregisterWhenRootDestroyed;
    }
}
