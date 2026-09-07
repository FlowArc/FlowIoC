#if UNITY_EDITOR
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.RootsContexts
{
    public class BaseScreenTestRoot<TContext> : Root<TContext>
        where TContext : BaseScreenContext, new()
    {
        /// <summary>
        /// A screen test scene keeps the screen's prefab in the scene so it can be edited there,
        /// and the run opens the screen from code instead - so the authored instance has to go
        /// before anything registers it.
        ///
        /// It goes here, in Awake, rather than a phase later. Cleaning up in
        /// AfterCreateBeforeStartContext put the destroy in the middle of Unity's Start phase: the
        /// screen's own ViewInjector could have started first, found no context started yet, and
        /// subscribed to OnContextReady - and StartContext raises that a few lines later, so the
        /// instance about to be destroyed got its Mediator built and registered. Undoing that meant
        /// calling ViewInjector.OnDestroy by hand, which is a Unity message and not ours to call.
        /// Awake is before every Start, so there is nothing to undo.
        /// </summary>
        protected override void BeforeCreateContext()
        {
            base.BeforeCreateContext();

            ClearAuthoredScreens();
        }

        /// <summary>
        /// Deactivated and then destroyed, in that order. Destroy is deferred to the end of the
        /// frame, and deactivating is what says - now, and without depending on when the frame
        /// ends - that no Start runs on this object.
        /// </summary>
        private static void ClearAuthoredScreens()
        {
            ScreenBody[] screens = FindObjectsByType<ScreenBody>(FindObjectsSortMode.None);

            for (int i = 0; i < screens.Length; i++)
            {
                GameObject screenObject = screens[i].gameObject;

                screenObject.SetActive(false);
                Destroy(screenObject);
            }
        }
    }
}
#endif