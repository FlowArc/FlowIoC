using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Model.Registry
{
    /// <summary>
    /// One screen as the service knows it: what its context declared, which context that was,
    /// and the instance if it has been loaded. The owner is what the loader hands the prefab's
    /// ViewInjector, so the view's mediator is created from the context that bound it rather than
    /// from whatever Root happens to sit above the layer.
    /// </summary>
    internal class ScreenEntry
    {
        public Type ViewType;
        public ScreenCVO Screen;
        public IContext Owner;

        /// <summary>Null until the screen is loaded; cleared again when it is unloaded.</summary>
        public IScreenBody Loaded;

        /// <summary>
        /// The load out for this entry, while one is. Two callers can reach an entry before its
        /// screen exists - an Open, and a preload that arrives while the Open's load is still out,
        /// or the other way round - and they await this one load rather than each instantiating a
        /// copy. A completed task left here is stale and ignored.
        /// </summary>
        public Task<IScreenBody> Loading;
    }
}