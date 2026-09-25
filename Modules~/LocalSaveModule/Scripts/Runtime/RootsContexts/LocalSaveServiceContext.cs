using FlowIoC.BaseModule.Contexts;
using Modules.LocalSaveModule.Models;
using Modules.LocalSaveModule.Services;

namespace Modules.LocalSaveModule.RootsContexts
{
    public class LocalSaveServiceContext : Context
    {
        private ILocalSaveModel _model;

        public override void InjectionBindings()
        {
            base.InjectionBindings();

            // Ticking IsTest on the Root binds the dummy instead, so a test scene neither reads the
            // developer's save file nor writes one. The dummy is an Editor affordance only: a build
            // always gets the real service.
            InjectionBinder.Bind<ILocalSaveService, LocalSaveService, DummyLocalSaveService>();

            // Cross-context because the Root reaches the model directly on quit, and because the
            // steps a game binds - ILocalSaveService.Commands.Save and SaveAll - run in the game's
            // own context and write through it.
            _model = InjectionBinderCrossContext.Bind<ILocalSaveModel, LocalSaveModel>();
        }

        /// <summary>
        /// A backgrounded mobile app is often killed without ever quitting cleanly, so the pause is
        /// the last reliable moment to write. Called on the model rather than dispatched, for the
        /// reason the Root gives on quit: a command dispatched while the app is going away may not
        /// run before it does.
        ///
        /// A pause during the boot can come before the bindings: there is no model yet, and
        /// nothing has been read that could be saved.
        /// </summary>
        public override void PauseContext()
        {
            base.PauseContext();

            _model?.SaveAll();
        }
    }
}
