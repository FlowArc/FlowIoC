using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Layer;
using FlowIoC.ScreenModule.Model.Config;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;

namespace FlowIoC.ScreenModule.Service.Sub
{
    internal class SetupSubService
    {
        [Inject] private IScreenConfigModel _screenConfigModel { get; set; }

        public void SetupScreen(IScreenBody screenBody)
        {
            var manager = _screenConfigModel.GetScreenManager(screenBody.Data.ManagerId);
            
            if (screenBody.Data.LayerIndex < 0 || screenBody.Data.LayerIndex >= manager.ScreenLayerList.Count)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ScreenService] Invalid layer index: {screenBody.Data.LayerIndex} for screen: {screenBody.Data.ScreenType.Name}");
                return;
            }

            var layer = manager.ScreenLayerList[screenBody.Data.LayerIndex];
            if (layer == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ScreenService] Layer is null at index: {screenBody.Data.LayerIndex}");
                return;
            }
            screenBody.BeforeScreenActivation();
            SetupRectTransform(screenBody, layer);
            screenBody.AfterScreenActivation();
            
            FlowLogger.Log(SystemLogType.Screen,$"[ScreenService] Screen {screenBody.Data.ScreenType.Name} setup completed - Layer: {screenBody.Data.LayerIndex}, Manager: {screenBody.Data.ManagerId}, Parent: {layer.transform.name}");
        }
        
        private void SetupRectTransform(IScreenBody screenBody, ScreenLayer layer)
        {
            RectTransform rectTransform = screenBody.transform as RectTransform;
            if (rectTransform == null)
            {
                FlowLogger.LogError(SystemLogType.Screen,$"[ScreenService] Screen {screenBody.GetType().Name} does not have a RectTransform component");
                return;
            }

            rectTransform.SetParent(layer.transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

    }
} 