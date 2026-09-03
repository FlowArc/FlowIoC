using System;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using Object = UnityEngine.Object;

namespace FlowIoC.BaseModule.ViewsMediators.View.Data
{
    /// <summary>
    /// What the injector knows about one view on the object: which Context it belongs to, and
    /// whether it has been handed to a Mediator yet.
    ///
    /// It wears the Mediator colour for the reason the injector does - these fields decide which
    /// Mediator a View ends up with, and a Mediator is not a component.
    /// </summary>
    [Serializable]
    [FlowHeader(FlowRole.Mediator)]
    public class ViewInjectorData
    {
        /// <summary>The view this entry describes. One entry per IView on the object.</summary>
        public Object View;

        /// <summary>
        /// Whether the view is handed to its Mediator as soon as its Context is ready. Off leaves
        /// the view alone until something calls Register on it.
        /// </summary>
        public bool AutoRegister = true;

        /// <summary>
        /// Whether the view itself is injected as well as mediated. A View holds scene references
        /// and raw input, so it rarely needs anything injected; the Mediator is what gets the
        /// module's models and signals.
        /// </summary>
        public bool InjectableView;

        /// <summary>
        /// How the view finds its Context. Bubbling up the hierarchy is what a view authored
        /// under its module's Root wants; the other two name a Root that is somewhere else.
        /// </summary>
        public ViewContextSource ContextSource = ViewContextSource.BubbleUp;

        /// <summary>
        /// The Root to register against, when the source is Selected Root. A scene reference: a
        /// prefab asset cannot hold one, so a prefab names its Root instead.
        /// </summary>
        public RootBase SelectedRoot;

        /// <summary>
        /// The GameObject name of the Root to register against, when the source is Root Name.
        /// Resolved when the object starts, so it reaches a Root a prefab could not reference.
        /// </summary>
        public string RootName;

        /// <summary>Whether the view has been handed to its Mediator. Written by the injector.</summary>
        public bool IsRegistered;
    }
}
