using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using Modules.WorldPointerModule.Data.ValueObjects;
using Modules.WorldPointerModule.Enums;
using Modules.WorldPointerModule.Models;
using Modules.WorldPointerModule.Services.Sub;
using UnityEngine;

namespace Modules.WorldPointerModule.Services
{
    /// <summary>
    /// The two registries and the one LateUpdate. Targets are keyed by id and Transform, displays
    /// by id; a target is matched the moment both exist and waits otherwise, keeping its last
    /// content and its last Show or Hide. Each frame every matched target is projected through the
    /// camera, judged against its display's frame, placed on its indicator's parent plane and told
    /// its state when that changed. Tick is public so a test can drive a frame by hand.
    /// </summary>
    public class WorldPointerService : IWorldPointerService, IConstructable
    {
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [Inject] private IWorldPointerModel _model { get; set; }

        private readonly List<Target> _targets = new();
        private readonly Dictionary<string, WorldPointerDisplaySlot> _displays = new();
        private Camera _camera;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        /// <summary>How many targets are registered, matched or waiting. For the tests and the test scene; not on the interface.</summary>
        public int Count => _targets.Count;

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

        // ---------------------------------------------------------------- the world side

        public bool RegisterTarget(string id, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrEmpty(id) || target == null)
            {
                FlowLogger.LogError("RegisterTarget - a target needs an id and a Transform, and one of them is missing. "
                                    + $"Registered from {file}:{line}.");
                return false;
            }

            if (Find(id, target) != null)
            {
                FlowLogger.LogError($"RegisterTarget - '{target.name}' is already registered under '{id}'. Two pointers on one "
                                    + $"object use two ids. Registered from {file}:{line}.", target);
                return false;
            }

            var entry = new Target {Id = id, Transform = target};
            _targets.Add(entry);

            if (_displays.TryGetValue(id, out WorldPointerDisplaySlot slot))
                Match(entry, slot);

            PublishStatus();
            return true;
        }

        public void UnregisterTarget(string id, Transform target)
        {
            Target entry = Find(id, target);
            if (entry == null) return;

            Unmatch(entry);
            _targets.Remove(entry);
            PublishStatus();
        }

        public void SetContent<TContent>(string id, Transform target, TContent content,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Target entry = FindForRequest(id, target, "SetContent", file, line);
            if (entry == null) return;

            if (_displays.TryGetValue(id, out WorldPointerDisplaySlot slot) && !slot.Accepts(content))
            {
                FlowLogger.LogError($"SetContent - the display of '{id}' ({slot.Name}) shows {slot.ContentType.Name}, "
                                    + $"not {typeof(TContent).Name}. Sent from {file}:{line}.", target);
                return;
            }

            entry.Content = content;
            entry.HasContent = true;

            if (entry.Indicator != null)
                entry.Slot.SetContent(entry.Indicator, content);

            PublishStatus();
        }

        public void Show(string id, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Target entry = FindForRequest(id, target, "Show", file, line);
            if (entry == null || entry.Visible) return;

            entry.Visible = true;
            PublishStatus();
        }

        public void Hide(string id, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Target entry = FindForRequest(id, target, "Hide", file, line);
            if (entry == null || !entry.Visible) return;

            entry.Visible = false;
            if (entry.Indicator != null) Report(entry, WorldPointerState.Hidden);
            PublishStatus();
        }

        public void UnregisterAll()
        {
            foreach (Target entry in _targets)
                Unmatch(entry);

            _targets.Clear();
            PublishStatus();
        }

        // ---------------------------------------------------------------- the screen side

        public bool RegisterDisplay<TContent>(string id, IWorldPointerDisplay<TContent> display,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrEmpty(id) || display == null || display is Object unityObject && unityObject == null)
            {
                FlowLogger.LogError("RegisterDisplay - a display needs an id and a display, and one of them is missing. "
                                    + $"Registered from {file}:{line}.");
                return false;
            }

            var slot = new WorldPointerDisplaySlot<TContent>(display);

            if (_displays.TryGetValue(id, out WorldPointerDisplaySlot existing))
            {
                FlowLogger.LogError($"RegisterDisplay - '{id}' is already drawn by {existing.Name}; {slot.Name} is refused. "
                                    + $"One id has one display. Registered from {file}:{line}.", display as Object);
                return false;
            }

            foreach (Target entry in _targets)
            {
                if (entry.Id != id || !entry.HasContent || slot.Accepts(entry.Content)) continue;

                FlowLogger.LogError($"RegisterDisplay - {slot.Name} shows {typeof(TContent).Name}, but '{entry.Transform.name}' "
                                    + $"under '{id}' holds {entry.Content.GetType().Name}. Registered from {file}:{line}.",
                    display as Object);
                return false;
            }

            _displays.Add(id, slot);

            foreach (Target entry in _targets)
                if (entry.Id == id)
                    Match(entry, slot);

            PublishStatus();
            return true;
        }

        public void UnregisterDisplay(IWorldPointerDisplay display)
        {
            if (display == null) return;

            foreach (KeyValuePair<string, WorldPointerDisplaySlot> pair in _displays)
            {
                if (!ReferenceEquals(pair.Value.Display, display)) continue;

                RemoveDisplay(pair.Key);
                PublishStatus();
                return;
            }
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

        // ---------------------------------------------------------------- the frame

        /// <summary>
        /// One frame for every target. A destroyed display is dropped first, and its targets wait.
        /// Walked backwards so an entry can be dropped mid-walk: a destroyed target is unregistered,
        /// its indicator released; a destroyed indicator is let go and the target waits for the next
        /// display. No camera at all leaves every indicator Hidden until one turns up.
        /// </summary>
        public void Tick(float deltaTime)
        {
            bool changed = DropDestroyedDisplays();
            Camera camera = Camera;

            for (int i = _targets.Count - 1; i >= 0; i--)
            {
                Target entry = _targets[i];

                if (entry.Transform == null)
                {
                    Unmatch(entry);
                    _targets.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (entry.Indicator == null) continue;

                if (entry.Indicator is Object indicatorObject && indicatorObject == null || entry.Indicator.Rect == null)
                {
                    entry.Indicator = null;
                    entry.Slot = null;
                    changed = true;
                    continue;
                }

                if (!entry.Visible) continue;

                if (camera == null)
                {
                    changed |= Report(entry, WorldPointerState.Hidden);
                    continue;
                }

                changed |= Step(entry, camera, deltaTime);
            }

            if (changed) PublishStatus();
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        /// <summary>
        /// The design's frame step. Behind the camera Unity's projection is mirrored through the
        /// centre, so it is mirrored back before anything reads it; the arrow is turned with a
        /// local rotation, which is right in both canvas modes where a world-space up is not.
        /// True when the indicator was told a new state.
        /// </summary>
        private static bool Step(Target entry, Camera camera, float deltaTime)
        {
            WorldPointerOptionsCVO options = entry.Slot.Options;

            Vector3 screen = camera.WorldToScreenPoint(entry.Transform.position + options.WorldOffset);
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
                        if (options.RotateArrow) AimArrow(entry, direction, options.SmoothSpeed, deltaTime);
                    }

                    place = true;
                    break;

                default:
                    state = WorldPointerState.InFrame;
                    place = true;
                    break;
            }

            if (place) Place(entry, point, options.SmoothSpeed, deltaTime);
            return Report(entry, state);
        }

        /// <summary>Local up along the direction: atan2 gives the angle of +x, and up is a quarter turn on.</summary>
        private static void AimArrow(Target entry, Vector2 direction, float speed, float deltaTime)
        {
            RectTransform pivot = entry.Indicator.ArrowPivot;
            if (pivot == null) return;

            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);

            pivot.localRotation = speed > 0f
                ? Quaternion.Slerp(pivot.localRotation, rotation, 1f - Mathf.Exp(-speed * deltaTime))
                : rotation;
        }

        /// <summary>
        /// By world point on the parent's plane rather than by anchoredPosition against the root,
        /// which is what makes any parent and any anchors correct in both canvas modes.
        /// </summary>
        private static void Place(Target entry, Vector2 point, float speed, float deltaTime)
        {
            RectTransform rect = entry.Indicator.Rect;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, point, entry.CanvasCamera, out Vector3 world))
                return;

            rect.position = speed > 0f
                ? Vector3.Lerp(rect.position, world, 1f - Mathf.Exp(-speed * deltaTime))
                : world;
        }

        /// <summary>Told on the first tick, then only on a change. A destroyed indicator is not told anything.</summary>
        private static bool Report(Target entry, WorldPointerState state)
        {
            if (entry.HasState && entry.State == state) return false;

            entry.HasState = true;
            entry.State = state;

            if (entry.Indicator is Object unityObject && unityObject == null) return true;

            entry.Indicator.SetState(state);
            return true;
        }

        // ---------------------------------------------------------------- matching

        /// <summary>
        /// Takes an indicator from the display for one target and replays what the target was told
        /// while it waited. An indicator the display could not give, or one with no Canvas above
        /// it, is reported against the display and handed back; the target keeps waiting.
        /// </summary>
        private static void Match(Target entry, WorldPointerDisplaySlot slot)
        {
            IWorldPointerIndicator indicator = slot.Acquire();

            if (indicator == null || indicator.Rect == null)
            {
                FlowLogger.LogError($"{slot.Name} gave no indicator with a RectTransform for '{entry.Transform.name}' under "
                                    + $"'{entry.Id}', so nothing points at it.", slot.Display as Object);
                return;
            }

            // Inactive included: a pooled indicator is often handed out still switched off.
            Canvas canvas = indicator.Rect.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                FlowLogger.LogError($"{slot.Name} gave the indicator '{indicator.Rect.name}' with no Canvas above it, so it "
                                    + "cannot be placed. Acquire has to hand out an element parented under the screen.",
                    indicator.Rect.gameObject);
                slot.Release(indicator);
                return;
            }

            Canvas rootCanvas = canvas.rootCanvas;

            entry.Slot = slot;
            entry.Indicator = indicator;
            entry.CanvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            entry.HasState = false;

            if (entry.HasContent)
                slot.SetContent(indicator, entry.Content);

            if (!entry.Visible)
                Report(entry, WorldPointerState.Hidden);
        }

        /// <summary>Hands the indicator back to its display. The display decides how it goes; nothing is told Hidden.</summary>
        private static void Unmatch(Target entry)
        {
            if (entry.Indicator == null) return;

            IWorldPointerIndicator indicator = entry.Indicator;
            WorldPointerDisplaySlot slot = entry.Slot;

            entry.Indicator = null;
            entry.Slot = null;
            entry.HasState = false;

            if (slot.IsDestroyed) return;
            if (indicator is Object unityObject && unityObject == null) return;

            slot.Release(indicator);
        }

        private void RemoveDisplay(string id)
        {
            foreach (Target entry in _targets)
                if (entry.Id == id)
                    Unmatch(entry);

            _displays.Remove(id);
        }

        private bool DropDestroyedDisplays()
        {
            List<string> destroyed = null;

            foreach (KeyValuePair<string, WorldPointerDisplaySlot> pair in _displays)
                if (pair.Value.IsDestroyed)
                    (destroyed ??= new List<string>()).Add(pair.Key);

            if (destroyed == null) return false;

            foreach (string id in destroyed)
                RemoveDisplay(id);

            return true;
        }

        private Target Find(string id, Transform target)
        {
            foreach (Target entry in _targets)
                if (entry.Id == id && entry.Transform == target)
                    return entry;

            return null;
        }

        private Target FindForRequest(string id, Transform target, string request, string file, int line)
        {
            Target entry = Find(id, target);
            if (entry != null) return entry;

            string name = target != null ? target.name : "null";
            FlowLogger.LogError($"{request} - '{name}' is not registered under '{id}'. RegisterTarget comes first. "
                                + $"Sent from {file}:{line}.", target);
            return null;
        }

        // ---------------------------------------------------------------- status

        /// <summary>Rewrites RD_WorldPointer from the registries: every id with a display or a target, one row each.</summary>
        private void PublishStatus()
        {
            if (_model == null) return;

            var channels = new List<WorldPointerChannelRVO>();
            var byId = new Dictionary<string, WorldPointerChannelRVO>();

            foreach (KeyValuePair<string, WorldPointerDisplaySlot> pair in _displays)
                byId[pair.Key] = Channel(channels, pair.Key, pair.Value.Name);

            foreach (Target entry in _targets)
            {
                if (!byId.TryGetValue(entry.Id, out WorldPointerChannelRVO channel))
                    byId[entry.Id] = channel = Channel(channels, entry.Id, string.Empty);

                bool shown = entry.Indicator != null;
                bool serializable = entry.HasContent && entry.Content != null && entry.Content.GetType().IsClass
                                    && entry.Content is not string && entry.Content is not Object;

                channel.Targets.Add(new WorldPointerTargetRVO
                {
                    Target = entry.Transform,
                    Visible = entry.Visible,
                    Shown = shown,
                    State = shown && entry.HasState ? entry.State : WorldPointerState.Hidden,
                    ContentText = entry.HasContent ? entry.Content?.ToString() ?? "null" : string.Empty,
                    Content = serializable ? entry.Content : null
                });
            }

            _model.Publish(channels);
        }

        private static WorldPointerChannelRVO Channel(List<WorldPointerChannelRVO> channels, string id, string display)
        {
            var channel = new WorldPointerChannelRVO {Id = id, Display = display};
            channels.Add(channel);
            return channel;
        }

        private sealed class Target
        {
            public string Id;
            public Transform Transform;
            public object Content;
            public bool HasContent;
            public bool Visible = true;
            public WorldPointerDisplaySlot Slot;
            public IWorldPointerIndicator Indicator;
            public Camera CanvasCamera;
            public bool HasState;
            public WorldPointerState State;
        }
    }
}