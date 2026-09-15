#if UNITY_EDITOR

using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.LocalSaveTestModule.Data.UnityObjects;
using Modules.LocalSaveModule.LocalSaveTestModule.RootsContexts;
using UnityEngine;

namespace Modules.LocalSaveModule.LocalSaveTestModule.Models
{
    public class LocalSaveTestModel : ILocalSaveTestModel, IConstructable
    {
        [Inject(nameof(LocalSaveTestContext))] private GameObject _root { get; set; }

        private PD_LocalSaveProbe _probe;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        /// <summary>
        /// Runs after LocalSaveModule's own PostConstruct, because that Root's InitializeOrder is
        /// lower - which is the whole thing this scene exists to show. The probe already holds
        /// whatever the last session saved by the time this reads it.
        /// </summary>
        public void PostConstruct() =>
            _probe = _root.GetComponent<RootAdapter>().GetScriptable<PD_LocalSaveProbe>();

        public int Counter => _probe.Counter;

        public void Increment() => _probe.Counter++;
    }
}

#endif
