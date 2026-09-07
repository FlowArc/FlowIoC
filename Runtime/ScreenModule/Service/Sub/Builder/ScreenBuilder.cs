using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    /// <summary>
    /// What <c>Open&lt;T&gt;()</c> hands back: everything one screen needs to be shown, and nothing
    /// belonging to any other. The checks that can refuse the open - a screen already on screen, a
    /// layer already occupied - run at <c>Show()</c>, because the calls in between are what decide
    /// whether being refused is the right answer. So does the pool: an open that never reaches
    /// Show has taken nothing out of it.
    /// </summary>
    internal sealed class ScreenBuilder : IScreenBuilder
    {
        private readonly IScreenRuntimeModel _runtimeModel;
        private readonly ShowSubService _show;
        private readonly HideSubService _hide;

        // Null when Open already refused - a screen registered nowhere - and said so.
        private readonly ScreenVO _screenData;

        private bool _shown;

        internal ScreenBuilder(ScreenVO screenData, IScreenRuntimeModel runtimeModel, ShowSubService show, HideSubService hide)
        {
            _screenData = screenData;
            _runtimeModel = runtimeModel;
            _show = show;
            _hide = hide;
        }

        #region The chain

        public IScreenBuilder OpenInLayer(int layerIndex)
        {
            if (_screenData != null)
                _screenData.LayerIndex = layerIndex;

            return this;
        }

        public IScreenBuilder ForceOpenAtDuplication(bool forceOpenForDuplicationWithHideAnim = false)
        {
            if (_screenData == null)
                return this;

            _screenData.ForceOpenAtDuplication = true;
            _screenData.ForceOpenForDuplicationWithHideAnim = forceOpenForDuplicationWithHideAnim;

            return this;
        }

        public IScreenBuilder ForceOpenAtFullLayer(bool forceOpenForLayerWithHideAnim = false)
        {
            if (_screenData == null)
                return this;

            _screenData.ForceOpenAtFullLayer = true;
            _screenData.ForceOpenAtFullLayerWithHideAnim = forceOpenForLayerWithHideAnim;

            return this;
        }

        public IScreenBuilder SetParameters(params object[] parameters)
        {
            if (_screenData != null)
                _screenData.Parameters = parameters;

            return this;
        }

        public IScreenBuilder AddToHistory()
        {
            if (_screenData != null)
                _screenData.AddToHistory = true;

            return this;
        }

        public IScreenBuilder SkipShowAnimation()
        {
            if (_screenData != null)
                _screenData.HasShowAnimation = false;

            return this;
        }

        public IScreenBuilder SkipHideAnimation()
        {
            if (_screenData != null)
                _screenData.HasHideAnimation = false;

            return this;
        }

        #endregion

        #region Show

        public async Task<IScreenBody> Show() => await Show<IScreenBody>();

        public async Task<T> Show<T>() where T : IScreenBody
        {
            if (!CanShow())
                return default;

            // Taken from the pool here and not at Open. A pooled instance carries the data of its
            // last opening, so it takes this opening's instead, with its pool state carried over.
            if (_runtimeModel.GetScreen(_screenData.ManagerId, _screenData.ScreenType, out IScreenBody pooled))
            {
                _screenData.State = pooled.Data.State;
                pooled.Data = _screenData;
                return _show.ShowPooledScreen<T>(pooled);
            }

            return await _show.ShowNewScreen<T>(_screenData);
        }

        /// <summary>
        /// Whether this open still stands. A builder shows once: chaining a second Show onto the
        /// same one would open a screen that is already open, and the answer to that is the
        /// duplication check rather than a second registration.
        /// </summary>
        private bool CanShow()
        {
            if (_screenData == null)
                return false;

            if (_shown)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Show] {_screenData.ScreenType.Name} has already been shown from this Open. " +
                    "Call Open again for a second screen.");
                return false;
            }

            _shown = true;

            if (CheckScreenDuplication()) return false;
            if (CheckIsLayerFull()) return false;

            return true;
        }

        private bool CheckScreenDuplication()
        {
            if (!_runtimeModel.IsScreenActive(_screenData.ScreenType, _screenData.ManagerId, out IScreenBody screenBody))
                return false;

            if (!_screenData.ForceOpenAtDuplication)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService] Manager({_screenData.ManagerId}) Screen: {_screenData.ScreenType.Name} is already active");
                return true;
            }

            FlowLogger.LogWarning(SystemLogType.Screen,
                $"[ScreenService.Builder] Manager({_screenData.ManagerId}) Screen {_screenData.ScreenType.Name} is forced open!!!");

            _hide.Screen(screenBody, !_screenData.ForceOpenForDuplicationWithHideAnim);
            return false;
        }

        private bool CheckIsLayerFull()
        {
            if (!_runtimeModel.IsLayerFull(_screenData.LayerIndex, _screenData.ManagerId, out _))
                return false;

            if (!_screenData.ForceOpenAtFullLayer)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Builder] Manager({_screenData.ManagerId}) Layer {_screenData.LayerIndex} is not empty");
                return true;
            }

            FlowLogger.LogWarning(SystemLogType.Screen,
                $"[ScreenService.Builder] Manager({_screenData.ManagerId}) Layer {_screenData.LayerIndex} is forced open!!!");

            _hide.ScreenInLayer(_screenData.LayerIndex, _screenData.ManagerId, !_screenData.ForceOpenAtFullLayerWithHideAnim);
            return false;
        }

        #endregion
    }
}
