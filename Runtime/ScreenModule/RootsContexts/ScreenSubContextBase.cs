using System;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Service;
using FlowIoC.ScreenModule.Signals;

namespace FlowIoC.ScreenModule.RootsContexts
{
    /// <summary>
    /// What every screen context is, with the view and mediator types left out. The Root that
    /// lists a screen context has to reach it without knowing those two types, and so does the
    /// inspector that draws it, which is why the declaration and everything done with it live
    /// here rather than on the generic class.
    ///
    /// ScreenViewType is internal and abstract, so this class cannot be derived from outside the
    /// package - a screen context derives from ScreenSubContext&lt;TView, TMediator&gt;, which
    /// supplies it.
    /// </summary>
    public abstract class ScreenSubContextBase : Context, ISubContextOverridable
    {
        protected IScreenService _screenService;

        private ScreenServiceInternalSignals _internalSignals;
        private ScreenCVO _resolved;

        /// <summary>
        /// Where the prefab comes from and how the screen behaves. Abstract rather than defaulted:
        /// a context that does not say where its prefab lives should not compile, instead of
        /// failing at the first Open.
        /// </summary>
        protected abstract ScreenCVO Screen { get; }

        internal abstract Type ScreenViewType { get; }

        /// <summary>What the context's own code declares, built fresh on every read.</summary>
        internal ScreenCVO Declaration => Screen;

        /// <summary>
        /// What is registered: the declaration, unless the Root listing this context overrode it.
        /// Pinned on first read because Screen hands back a new object every time.
        /// </summary>
        internal ScreenCVO Resolved => _resolved ??= Screen;

        /// <summary>
        /// The Root listing this context replaces the five values a scene is allowed to decide.
        /// Load is copied from the declaration rather than taken from the entry: where a prefab
        /// lives is the module's business and not the scene's, and this is where that is enforced.
        /// </summary>
        void ISubContextOverridable.ApplyOverride(in SubContextData data)
        {
            if (!data.OverrideScreen)
                return;

            ScreenCVO declaration = Screen;

            _resolved = new ScreenCVO
            {
                Load = declaration.Load,
                ManagerId = data.ScreenManagerId,
                Layer = data.ScreenLayer,
                Tag = data.ScreenTag,
                HasShowAnimation = data.ScreenHasShowAnimation,
                HasHideAnimation = data.ScreenHasHideAnimation
            };
        }

        protected override void CoreBindings()
        {
            base.CoreBindings();

            _screenService = InjectionBinderCrossContext.GetInstance<IScreenService>();
            _internalSignals = InjectionBinderCrossContext.GetInstance<ScreenServiceInternalSignals>();
        }

        // Setup is the phase that may reach another module, and it runs once every Root has bound -
        // so the service's commands are in place whichever Root started first.
        public override void Setup()
        {
            base.Setup();

            if (_internalSignals == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"{GetType().Name} could not reach the screen service. Is ScreenServiceRoot in the scene?");
                return;
            }

            _internalSignals.RegisterScreen.Dispatch(new ScreenEntry
            {
                ViewType = ScreenViewType,
                Screen = Resolved,
                Owner = this
            });
        }

        public override void DestroyContext()
        {
            _internalSignals?.UnRegisterScreen.Dispatch(Resolved.ManagerId, ScreenViewType);

            base.DestroyContext();
        }
    }
}