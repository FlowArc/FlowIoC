using System.Threading.Tasks;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.Model.Runtime;
using FlowIoC.ScreenModule.ViewsMediators.Screen;

namespace FlowIoC.ScreenModule.Service.Sub.Builder
{
    public class ScreenBuilderSubService : IScreenBuilderSubService
    {
        [Inject] private IScreenRegistryModel _registry { get; set; }
        [Inject] private IScreenRuntimeModel _screenRuntimeModel { get; set; }
        [Inject] private ShowSubService _show { get; set; }
        [Inject] private HideSubService _hide { get; set; }

        private ScreenVO _currentScreenData;
        private IScreenBody _currentScreenBody;
        private bool _openAborted;

        IScreenBuilderSubService IScreenBuilderSubService.Open<T>(int managerId)
        {
            _openAborted = false;
            if (_screenRuntimeModel.GetScreen<T>(managerId, out var screen))
            {
                _currentScreenBody = screen;
                _currentScreenData = _currentScreenBody.Data;
                _currentScreenData.ManagerId = managerId;
                _registry.CopyDataFromConfig(_currentScreenData);
            }
            else
            {
                _currentScreenData = new ScreenVO {ScreenType = typeof(T), ManagerId = managerId};

                ScreenEntry entry = _registry.GetEntry(managerId, typeof(T));
                if (entry == null)
                {
                    FlowLogger.LogError(SystemLogType.Screen,
                        $"[ScreenService.Open] {typeof(T).Name} aborted: not registered at manager {managerId}. Is the Root of the module that owns it in the scene?");
                    _openAborted = true;
                    return this;
                }

                _registry.CopyDataFromConfig(_currentScreenData, entry.Screen);
            }

            FlowLogger.Log(SystemLogType.Screen, $"[ScreenService.Open] {_currentScreenData.ScreenType.Name}");
            return this;
        }

        public IScreenBuilderSubService OpenInLayer(int layerIndex)
        {
            _currentScreenData.LayerIndex = layerIndex;
            return this;
        }

        public IScreenBuilderSubService ForceOpenAtDuplication(bool forceOpenForDuplicationWithHideAnim = false)
        {
            _currentScreenData.ForceOpenAtDuplication = true;
            _currentScreenData.ForceOpenForDuplicationWithHideAnim = forceOpenForDuplicationWithHideAnim;
            return this;
        }

        public IScreenBuilderSubService ForceOpenAtFullLayer(bool forceOpenForLayerWithHideAnim = false)
        {
            _currentScreenData.ForceOpenAtFullLayer = true;
            _currentScreenData.ForceOpenAtFullLayerWithHideAnim = forceOpenForLayerWithHideAnim;
            return this;
        }

        public IScreenBuilderSubService SetParameters(params object[] parameters)
        {
            _currentScreenData.Parameters = parameters;
            return this;
        }

        public IScreenBuilderSubService AddToHistory()
        {
            _currentScreenData.AddToHistory = true;
            return this;
        }

        public IScreenBuilderSubService SkipShowAnimation()
        {
            _currentScreenData.HasShowAnimation = false;
            return this;
        }

        public IScreenBuilderSubService SkipHideAnimation()
        {
            _currentScreenData.HasHideAnimation = false;
            return this;
        }

        public async Task<IScreenBody> Show()
        {
            if (BeforeShowScreen()) return default;
            if (_currentScreenBody == null)
            {
                return await _show.ShowNewScreen<IScreenBody>();
            }
            else
                return await _show.ShowPooledScreen<IScreenBody>();
        }

        public async Task<T> Show<T>() where T : IScreenBody
        {
            if (BeforeShowScreen()) return default;
            if (_currentScreenBody == null)
            {
                return await _show.ShowNewScreen<T>();
            }
            else
                return await _show.ShowPooledScreen<T>();
        }

        private bool BeforeShowScreen()
        {
            if (_openAborted)
            {
                _openAborted = false;
                Reset();
                return true;
            }

            if (CheckScreenDuplication()) return true;
            if (CheckIsLayerFull()) return true;
            return false;
        }

        private bool CheckScreenDuplication()
        {
            if (!_screenRuntimeModel.IsScreenActive(_currentScreenData.ScreenType, _currentScreenData.ManagerId, out IScreenBody screenBody))
                return false;

            if (!_currentScreenData.ForceOpenAtDuplication)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService] Manager({_currentScreenData.ManagerId}) Screen: {_currentScreenData.ScreenType.Name} is already active");

                Reset();
                return true;
            }

            FlowLogger.LogWarning(SystemLogType.Screen,
                $"[ScreenService.Builder] Manager({_currentScreenData.ManagerId}) Screen {_currentScreenData.ScreenType.Name} is forced open!!!");

            _hide.Screen(screenBody, !_currentScreenData.ForceOpenForDuplicationWithHideAnim);
            return false;
        }

        private bool CheckIsLayerFull()
        {
            if (!_screenRuntimeModel.IsLayerFull(_currentScreenData.LayerIndex, _currentScreenData.ManagerId, out _)) return false;

            if (!_currentScreenData.ForceOpenAtFullLayer)
            {
                FlowLogger.LogError(SystemLogType.Screen,
                    $"[ScreenService.Builder] Manager({_currentScreenData.ManagerId}) Layer {_currentScreenData.LayerIndex} is not empty");

                Reset();
                return true;
            }

            FlowLogger.LogWarning(SystemLogType.Screen,
                $"[ScreenService.Builder] Manager({_currentScreenData.ManagerId}) Layer {_currentScreenData.LayerIndex} is forced open!!!");

            _hide.ScreenInLayer(_currentScreenData.LayerIndex, _currentScreenData.ManagerId, !_currentScreenData.ForceOpenAtFullLayerWithHideAnim);
            return false;
        }

        #region Internal Methods

        ScreenVO IScreenBuilderSubService.GetAndFlushScreenData()
        {
            var data = _currentScreenData;
            _currentScreenData = null;
            return data;
        }

        T IScreenBuilderSubService.GetAndFlushScreenBody<T>()
        {
            var body = _currentScreenBody;
            _currentScreenBody = null;
            return (T) body;
        }

        private void Reset()
        {
            if (_currentScreenBody != null)
                _screenRuntimeModel.AddToPassivePool(_currentScreenBody);

            _currentScreenBody = null;
            _currentScreenData = null;
        }

        #endregion
    }
}