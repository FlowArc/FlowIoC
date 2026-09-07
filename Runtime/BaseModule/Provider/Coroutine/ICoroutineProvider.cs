using System;
using System.Collections;

namespace FlowIoC.BaseModule.Provider.Coroutine
{
    public interface ICoroutineProvider
    {
        UnityEngine.Coroutine WaitForSeconds(float seconds, Action callback);
        UnityEngine.Coroutine WaitForSecondsRealTime(float seconds, Action callback);

        UnityEngine.Coroutine WaitForEndOfFrame(Action callback);
        UnityEngine.Coroutine WaitForEndOfFrames(int frameCount, Action callback);

        UnityEngine.Coroutine WaitUntil(Func<bool> condition, Action callback);
        
        UnityEngine.Coroutine StartCoroutine(IEnumerator enumerator);
        
        void StopCoroutine(UnityEngine.Coroutine coroutine);
        void StopCoroutine(IEnumerator enumerator);
        void StopCoroutine(string methodName);
        void StopAllCoroutines();
    }
}