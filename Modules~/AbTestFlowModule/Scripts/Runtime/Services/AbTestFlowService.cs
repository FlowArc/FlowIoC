using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AbTestFlowModule.Models;
using Modules.AbTestFlowModule.Signals;

namespace Modules.AbTestFlowModule.Services
{
    /// <summary>
    /// The module's unit of work, and what starts it. PostConstruct rather than Setup on purpose:
    /// PostConstruct runs during the binding pass, so a Root at a low enough InitializeOrder has
    /// its overrides written before any other module's own PostConstruct reads the config they
    /// change. Setup would be a frame too late. The service only says go - the deciding is the
    /// Command sequence bound to ResolveAbTests, read in the context.
    /// </summary>
    public class AbTestFlowService : IAbTestFlowService, IConstructable
    {
        [Inject] private IAbTestFlowModel _model { get; set; }

        [InjectSignal] private AbTestFlowSignals _signals { get; set; }

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct() => _signals.Incoming.ResolveAbTests.Dispatch();

        public string GetGroup(string abTestId) => _model.GetGroup(abTestId);

        public bool IsInGroup(string abTestId, string groupName)
        {
            string group = _model.GetGroup(abTestId);

            return group != null && group == groupName;
        }

#if UNITY_EDITOR
        public void RestoreEditorAssets() => _model.RestoreEditorAssets();
#endif
    }
}