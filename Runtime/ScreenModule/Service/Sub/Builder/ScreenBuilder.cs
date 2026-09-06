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
    /// whether being refused is the right answer.
    /// </summary>
    internal sealed class ScreenBuilder : IScreenBuilder
    {
        private readonly IScreenRuntimeModel _runtimeModel;
        private readonly ShowSubService _show;
        private readonly HideSubService _hide;

        private readonly ScreenVO _screenData;
        private readonly IScreenBody _pooledScreen;
        private readonly bool _aborted;

        private bool _shown;

        internal ScreenBuilder(ScreenVO screenData, IScreenBody pooledScreen, bool aborted,
            IScreenRuntimeModel runtimeModel, ShowSubService show, HideSubService hide)
        {
            _screenData = screenData;
            _pooledScreen = pooledScreen;
            _aborted = aborted;
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

            return _pooledScreen == null
                ? await _show.ShowNewScreen<T>(_screenData)
                : _show.ShowPooledScreen<T>(_pooledScreen);
        }

        /// <summary>
        /// Whether this open still stands. A builder shows once: chaining a second Show onto the
        /// same one would open a screen that is already open, and the answer to that is the
        /// duplication check rather than a second registration.
        /// </summary>
        private bool CanShow()
        {
            if (_aborted || _screenData == null)
            {
                ReturnPooledScreen();
                return false;
            }

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

                ReturnPooledScreen();
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

                ReturnPooledScreen();
                return true;
            }

            FlowLogger.LogWarning(SystemLogType.Screen,
                $"[ScreenService.Builder] Manager({_screenData.ManagerId}) Layer {_screenData.LayerIndex} is forced open!!!");

            _hide.ScreenInLayer(_screenData.LayerIndex, _screenData.ManagerId, !_screenData.ForceOpenAtFullLayerWithHideAnim);
            return false;
        }

        /// <summary>
        /// An open that came out of the pool and then did not happen has taken an instance nobody
        /// is going to show. It goes back, or the pool loses it.
        /// </summary>
        private void ReturnPooledScreen()
        {
            if (_pooledScreen != null)
                _runtimeModel.AddToPassivePool(_pooledScreen);
        }

        #endregion
    }
}
