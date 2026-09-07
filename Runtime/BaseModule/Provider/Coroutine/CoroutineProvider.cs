using System;
using System.Collections;
using FlowIoC.BaseModule.Attributes;
using UnityEngine;

namespace FlowIoC.BaseModule.Provider.Coroutine
{
    [HideInModelViewer]
    public class CoroutineProvider : MonoBehaviour, ICoroutineProvider
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        #region WaitForSeconds

        public UnityEngine.Coroutine WaitForSeconds(float seconds, Action callback)
        {
            IEnumerator _(float secs, Action call)
            {
                yield return new WaitForSeconds(secs);

                call?.Invoke();
            }

            return StartCoroutine(_(seconds, callback));
        }

        public UnityEngine.Coroutine WaitForSecondsRealTime(float seconds, Action callback)
        {
            IEnumerator _(float secs, Action call)
            {
                yield return new WaitForSecondsRealtime(secs);

                call?.Invoke();
            }

            return StartCoroutine(_(seconds, callback));
        }

        #endregion

        #region WaitEndOfFrame

        public UnityEngine.Coroutine WaitForEndOfFrame(Action callback)
        {
            IEnumerator _(Action call)
            {
                yield return new WaitForEndOfFrame();
                call?.Invoke();
            }

            return StartCoroutine(_(callback));
        }

        public UnityEngine.Coroutine WaitForEndOfFrames(int frameCount, Action callback)
        {
            IEnumerator _(int fC, Action call)
            {
                for (int ii = 0; ii < fC; ii++)
                    yield return new WaitForEndOfFrame();

                call?.Invoke();
            }


            return StartCoroutine(_(frameCount, callback));
        }

        #endregion

        public UnityEngine.Coroutine WaitUntil(Func<bool> condition, Action callback)
        {
            IEnumerator _(Func<bool> condition, Action call)
            {
                yield return new WaitUntil(condition);

                call?.Invoke();
            }

            return StartCoroutine(_(condition, callback));
        }
    }
}