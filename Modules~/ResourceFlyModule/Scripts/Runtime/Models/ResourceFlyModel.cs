using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.ResourceFlyModule.Data.UnityObjects;
using Modules.ResourceFlyModule.Data.ValueObjects;
using Modules.ResourceFlyModule.RootsContexts;
using UnityEngine;

namespace Modules.ResourceFlyModule.Models
{
    /// <summary>
    /// The resources by key and the sources by name. The saved value lives here only as last told;
    /// the shown value is always worked out from it, so there is no second copy to fall behind.
    /// </summary>
    internal class ResourceFlyModel : IResourceFlyModel, IConstructable
    {
        [Inject(nameof(ResourceFlyServiceContext))] private GameObject _root { get; set; }

        private readonly Dictionary<string, ResourceVO> _resources = new();
        private readonly Dictionary<string, RectTransform> _sources = new();

        public ResourceFlyOptionsCVO Options { get; private set; } = new();

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public void PostConstruct()
        {
            CD_ResourceFly config = _root.GetComponent<RootAdapter>().GetScriptable<CD_ResourceFly>();

            if (config == null)
            {
                FlowLogger.LogError("ResourceFlyModel found no CD_ResourceFly on the Root adapter: flights use the defaults.");
                return;
            }

            Options = config.Options;
        }

        public ResourceVO Get(string key)
        {
            if (!_resources.TryGetValue(key, out ResourceVO resource))
            {
                resource = new ResourceVO { Key = key };
                _resources[key] = resource;
            }

            return resource;
        }

        public void SetSource(string source, RectTransform rect) => _sources[source] = rect;

        public void RemoveSource(string source) => _sources.Remove(source);

        public bool TryGetSource(string source, out RectTransform rect) =>
            _sources.TryGetValue(source, out rect) && rect != null;
    }
}
