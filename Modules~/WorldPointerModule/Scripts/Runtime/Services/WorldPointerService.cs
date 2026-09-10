using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Services.Sub;
using UnityEngine;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// The registry and the one LateUpdate. Each frame every entry is projected through the
    /// camera, judged against its frame, placed on its parent's plane and told its state when
    /// that changed. Tick is public so a test can drive a frame by hand.
    /// </summary>
    public class WorldPointerService : IWorldPointerService, IConstructable
    {
        [Inject] private IUpdateProvider _updateProvider { get; set; }

        private readonly List<Entry> _entries = new();
        private int _nextId = 1;
        private Camera _camera;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        /// <summary>How many pointers are being followed. For the tests and the test scene; not on the interface.</summary>
        public int Count => _entries.Count;

        public Camera Camera
        {
            get
            {
                if (_camera == null) _camera = Camera.main;
                return _camera;
            }
            set => _camera = value;
        }

        public void PostConstruct() => _updateProvider.AddLateUpdate(LateUpdate);

        public void Deconstruct() => _updateProvider.RemoveLateUpdate(LateUpdate);

        public WorldPointerHandle Register(Transform target, IWorldPointerIndicator indicator,
            WorldPointerOptionsCVO options = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (target == null || indicator == null || indicator.Rect == null)
            {
                FlowLogger.LogError(FlowLogType.WorldPointerModule,
                    "Register - a pointer needs a target and an indicator with a RectTransform, and one of them "
                    + $"is missing. Registered from {file}:{line}.");
                return default;
            }

            // Inactive included: a pooled indicator is often registered while still switched off.
            Canvas canvas = indicator.Rect.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                FlowLogger.LogError(FlowLogType.WorldPointerModule,
                    $"Register - the indicator '{indicator.Rect.name}' has no Canvas above it, so it cannot be "
                    + $"placed. Parent it under a Canvas before registering it. Registered from {file}:{line}.",
                    indicator.Rect.gameObject);
                return default;
            }

            Canvas rootCanvas = canvas.rootCanvas;

            var entry = new Entry
            {
                Id = _nextId++,
                Target = target,
                Indicator = indicator,
                Options = options ?? new WorldPointerOptionsCVO(),
                CanvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera
            };

            _entries.Add(entry);

            return new WorldPointerHandle(entry.Id, this);
        }

        public void Unregister(WorldPointerHandle handle)
        {
            if (!handle.IsValid) return;

            int index = IndexOf(handle.Id);
            if (index < 0) return;

            Report(_entries[index], WorldPointerState.Hidden);
            _entries.RemoveAt(index);
        }

        public void UnregisterAll()
        {
            foreach (Entry entry in _entries)
                Report(entry, WorldPointerState.Hidden);

            _entries.Clear();
        }

        public bool TryProject(Vector3 worldPosition, RectTransform parent, out Vector3 pointOnCanvas)
        {
            pointOnCanvas = default;

            Camera camera = Camera;
            if (camera == null || parent == null) return false;

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) return false;

            Canvas canvas = parent.GetComponentInParent<Canvas>(true);
            Camera canvasCamera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.rootCanvas.worldCamera;

            return RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screen, canvasCamera, out pointOnCanvas);
        }

        /// <summary>
        /// One frame for every pointer. Walked backwards so an entry can be dropped mid-walk. A
        /// destroyed target or indicator drops its entry and is told Hidden, once, and nothing
        /// throws; no camera at all leaves every pointer Hidden until one turns up.
        /// </summary>
        public void Tick(float deltaTime)
        {
            Camera camera = Camera;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry entry = _entries[i];

                if (entry.Target == null || entry.Indicator.Rect == null)
                {
                    Report(entry, WorldPointerState.Hidden);
                    _entries.RemoveAt(i);
                    continue;
                }

                if (camera == null)
                {
                    Report(entry, WorldPointerState.Hidden);
                    continue;
                }

                Step(entry, camera, deltaTime);
            }
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>
        /// The design's frame step. Behind the camera Unity's projection is mirrored through the
        /// centre, so it is mirrored back before anything reads it; the arrow is turned with a
        /// local rotation, which is right in both canvas modes where a world-space up is not.
        /// </summary>
        private static void Step(Entry entry, Camera camera, float deltaTime)
        {
            WorldPointerOptionsCVO options = entry.Options;

            Vector3 screen = camera.WorldToScreenPoint(entry.Target.position + options.WorldOffset);
            bool behind = screen.z < 0f;
            Vector2 point = (Vector2) screen + options.ScreenOffset;

            var frame = new WorldPointerFrame(camera.pixelRect, options.ScreenMargins);
            if (behind) point = frame.Centre - (point - frame.Centre);

            bool inside = !behind && frame.Contains(point);

            WorldPointerState state;
            bool place;

            switch (options.OffScreen)
            {
                case OffScreenMode.Hide:
                    state = inside ? WorldPointerState.InFrame : WorldPointerState.Hidden;
                    place = inside;
                    break;

                case OffScreenMode.ClampToEdge:
                    if (inside)
                    {
                        state = WorldPointerState.InFrame;
                    }
                    else
                    {
                        state = WorldPointerState.OnEdge;
                        point = frame.ClampToEdge(point, out Vector2 direction);
                        if (options.RotateArrow) AimArrow(entry, direction, deltaTime);
                    }

                    place = true;
                    break;

                default:
                    state = WorldPointerState.InFrame;
                    place = true;
                    break;
            }

            if (place) Place(entry, point, deltaTime);
            Report(entry, state);
        }

        /// <summary>Local up along the direction: atan2 gives the angle of +x, and up is a quarter turn on.</summary>
        private static void AimArrow(Entry entry, Vector2 direction, float deltaTime)
        {
            RectTransform pivot = entry.Indicator.ArrowPivot;
            if (pivot == null) return;

            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            float speed = entry.Options.SmoothSpeed;

            pivot.localRotation = speed > 0f
                ? Quaternion.Slerp(pivot.localRotation, rotation, 1f - Mathf.Exp(-speed * deltaTime))
                : rotation;
        }

        /// <summary>
        /// By world point on the parent's plane rather than by anchoredPosition against the root,
        /// which is what makes any parent and any anchors correct in both canvas modes.
        /// </summary>
        private static void Place(Entry entry, Vector2 point, float deltaTime)
        {
            RectTransform rect = entry.Indicator.Rect;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, point, entry.CanvasCamera, out Vector3 world))
                return;

            float speed = entry.Options.SmoothSpeed;

            rect.position = speed > 0f
                ? Vector3.Lerp(rect.position, world, 1f - Mathf.Exp(-speed * deltaTime))
                : world;
        }

        /// <summary>Told on the first tick, then only on a change. A destroyed indicator is not told anything.</summary>
        private static void Report(Entry entry, WorldPointerState state)
        {
            if (entry.HasState && entry.State == state) return;

            entry.HasState = true;
            entry.State = state;

            if (entry.Indicator is UnityEngine.Object unityObject && unityObject == null) return;

            entry.Indicator.SetState(state);
        }

        private int IndexOf(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Id == id)
                    return i;

            return -1;
        }

        private sealed class Entry
        {
            public int Id;
            public Transform Target;
            public IWorldPointerIndicator Indicator;
            public WorldPointerOptionsCVO Options;
            public Camera CanvasCamera;
            public bool HasState;
            public WorldPointerState State;
        }
    }
}