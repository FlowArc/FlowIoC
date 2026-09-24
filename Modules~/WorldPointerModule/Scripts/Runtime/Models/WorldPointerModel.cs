using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.WorldPointerModule.Data.UnityObjects;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.RootsContexts;
using UnityEngine;

namespace Modules.WorldPointerModule.Models
{
    /// <summary>
    /// Takes RD_WorldPointer off the Root's adapter and empties it: a ScriptableObject edited in
    /// play mode keeps the edit on disk, so the last session's rows would otherwise show. The
    /// asset is optional - without it the service works and nothing is shown.
    /// </summary>
    internal class WorldPointerModel : IWorldPointerModel, IConstructable
    {
        [Inject(nameof(WorldPointerServiceContext))] private GameObject _root { get; set; }

        private RD_WorldPointer _status;

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
            {
                FlowLogger.LogError("WorldPointerServiceRoot has no RootAdapter, so RD_WorldPointer cannot be read; nothing is shown in it.", _root);
                return;
            }

            Load(adapter.GetScriptable<RD_WorldPointer>());
        }

        /// <summary>Holds the asset and empties it. Internal so a test loads one without a Root.</summary>
        internal void Load(RD_WorldPointer status)
        {
            _status = status;

            if (_status != null)
                _status.Channels.Clear();
        }

        public void Publish(List<WorldPointerChannelRVO> channels)
        {
            if (_status == null)
                return;

            _status.Channels.Clear();
            _status.Channels.AddRange(channels);
        }
    }
}
