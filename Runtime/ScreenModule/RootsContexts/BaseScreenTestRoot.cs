#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.Root;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.RootsContexts
{
    public class BaseScreenTestRoot<TContext> : Root<TContext>
        where TContext : BaseScreenContext, new()
    {
        protected override void AfterCreateBeforeStartContext()
        {
            base.AfterCreateBeforeStartContext();

            DestroyScreenObject();
        }

        private static void DestroyScreenObject()
        {
            List<ScreenBody> screens = FindObjectsByType<ScreenBody>(FindObjectsSortMode.None).ToList();

            for (int ii = 0; ii < screens.Count; ii++)
            {
                ScreenBody screen = screens[ii];
                screen.GetComponent<ViewInjector>().OnDestroy();
                Destroy(screen.gameObject);
            }
        }
    }
}
#endif