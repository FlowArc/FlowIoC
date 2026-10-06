#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// Whether any of an injector's views registered or dropped out since the last look, so its
    /// inspector repaints when a Registration badge has something new to say and not otherwise.
    ///
    /// The inspector used to ask Unity for a repaint on every frame of play, which redraws the whole
    /// Inspector window - the view, its other components and whatever Odin draws for them - and a
    /// selected view cost the game its frame rate.
    /// </summary>
    internal class ViewInjectorRegistrationWatch
    {
        private readonly List<bool> _last = new List<bool>();
        private bool _looked;

        internal bool Moved(ViewInjector injector)
        {
            if (injector == null)
                return false;

            int count = injector.viewDataList?.Count ?? 0;
            bool moved = !_looked || count != _last.Count;

            for (int i = 0; i < count; i++)
            {
                bool registered = injector.viewDataList[i] != null && injector.viewDataList[i].IsRegistered;

                if (i < _last.Count)
                {
                    moved |= _last[i] != registered;
                    _last[i] = registered;
                }
                else
                {
                    _last.Add(registered);
                }
            }

            if (_last.Count > count)
                _last.RemoveRange(count, _last.Count - count);

            _looked = true;

            return moved;
        }
    }
}
#endif
