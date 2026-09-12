#if UNITY_EDITOR
using System.Collections;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Coroutine;
using Modules.LoadingModule.Services;
using UnityEngine;

namespace Modules.LoadingModule.LoadingTestModule.Controllers
{
    /// <summary>
    /// Pretends to load for a number of seconds, reporting progress every frame, and ends the way
    /// the binding says: (step, seconds, fails). Bound in parallel, several of these are what a
    /// real boot looks like to the loading screen.
    /// </summary>
    internal class FakeLoadingStepCommand : Command<string, float, bool>
    {
        [Inject] private ILoadingService _loadingService { get; set; }
        [Inject] private ICoroutineProvider _coroutineProvider { get; set; }

        public override void Execute(string stepKey, float seconds, bool fails)
        {
            Retain();
            _coroutineProvider.StartCoroutine(Run(stepKey, seconds, fails));
        }

        private IEnumerator Run(string stepKey, float seconds, bool fails)
        {
            ILoadingStep step = _loadingService.Report(stepKey);
            step.Start();

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float fraction = Mathf.Clamp01(elapsed / seconds);
                step.Progress(fraction);
                step.Detail($"{Mathf.RoundToInt(fraction * 200f)} MB / 200 MB");
                yield return null;
            }

            if (fails)
            {
                step.Fail("the test asked for a failure");
                Stop();
                yield break;
            }

            step.Complete();
            Release();
        }
    }
}
#endif
