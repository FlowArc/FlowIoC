#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Modules.CountdownServiceModule.CountdownServiceTestModule.RootsContexts;
using Modules.CountdownServiceModule.CountdownServiceTestModule.ViewsMediators;

namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Editor
{
    /// <summary>
    /// Builds the scene this test module runs in. The scene is generated rather than shipped so
    /// that installing the module carries no binary asset whose references could arrive broken -
    /// run the menu item once and the scene is there, wired and ready to press Play on.
    /// </summary>
    internal class CountdownTestSceneBuilder
    {
        private const string ScenePath =
            "Assets/Modules/CountdownServiceModule/zTestModules/CountdownServiceTestModule/Scenes/CountdownServiceTestScene.unity";

        [MenuItem("Tools/FlowIoC/Modules/Countdown Service/Build Test Scene")]
        private static void Build()
        {
            if (System.IO.File.Exists(ScenePath)
                && !EditorUtility.DisplayDialog("Countdown Test Scene",
                    "The scene already exists and will be replaced. Continue?", "Replace", "Cancel"))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Both roots belong in the scene. The test module only knows how to ask for a
            // countdown; the service module's own context is what binds ICountdownService for it
            // to be given, so a scene with only the test root would have nothing to inject.
            new GameObject("CountdownServiceRoot",
                typeof(Modules.CountdownServiceModule.RootsContexts.CountdownServiceRoot));
            var testRoot = new GameObject("CountdownServiceTestRoot", typeof(CountdownServiceTestRoot))
                .GetComponent<CountdownServiceTestRoot>();

            Canvas canvas = CreateCanvas();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Text remaining = CreateLabel(canvas.transform, "RemainingLabel", "Remaining: -", 120f);
            Text elapsed = CreateLabel(canvas.transform, "ElapsedLabel", "Elapsed: -", 60f);
            Text status = CreateLabel(canvas.transform, "StatusLabel", "Idle", 0f);

            Button start = CreateButton(canvas.transform, "StartButton", "Start", -70f);
            Button stop = CreateButton(canvas.transform, "StopButton", "Stop", -130f);

            var viewObject = new GameObject("CountdownTestView", typeof(ViewInjector), typeof(CountdownTestView));
            var view = viewObject.GetComponent<CountdownTestView>();

            // A ViewInjector with an empty list registers nothing. This is the entry that tells it
            // which view to inject and which root owns the context to inject it from.
            viewObject.GetComponent<ViewInjector>().viewDataList = new List<ViewInjectorData>
            {
                new ViewInjectorData
                {
                    View = view,
                    AutoRegister = true,
                    InjectableView = true,
                    UseBubbleUp = false,
                    UseRootSelection = true,
                    SelectedRoot = testRoot
                }
            };

            // The view's references are private, which is what keeps the scene the only thing
            // that decides what it points at. SerializedObject is how an editor script writes
            // them without widening the view's surface for everyone else.
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_remainingLabel").objectReferenceValue = remaining;
            serialized.FindProperty("_elapsedLabel").objectReferenceValue = elapsed;
            serialized.FindProperty("_statusLabel").objectReferenceValue = status;
            serialized.FindProperty("_startButton").objectReferenceValue = start;
            serialized.FindProperty("_stopButton").objectReferenceValue = stop;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"<color=cyan>[CountdownTestSceneBuilder]</color> Scene written to {ScenePath}. Press Play to run it.");
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            return canvas;
        }

        private static Text CreateLabel(Transform parent, string name, string text, float y)
        {
            var label = new GameObject(name, typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);

            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 36;
            label.color = Color.white;
            // Unity 6 dropped the old built-in Arial; LegacyRuntime is the font that replaced it.
            // A null font still renders nothing rather than throwing, so the scene is built either way.
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var rect = label.rectTransform;
            rect.sizeDelta = new Vector2(600f, 60f);
            rect.anchoredPosition = new Vector2(0f, y);

            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, float y)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.9f);

            var rect = (RectTransform) buttonObject.transform;
            rect.sizeDelta = new Vector2(240f, 50f);
            rect.anchoredPosition = new Vector2(0f, y);

            Text label = CreateLabel(buttonObject.transform, "Label", text, 0f);
            label.fontSize = 24;
            label.rectTransform.sizeDelta = rect.sizeDelta;

            return buttonObject.GetComponent<Button>();
        }
    }
}
#endif
