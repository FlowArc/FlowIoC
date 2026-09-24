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
    /// The front of the module. The registry is the Model's and one target's frame is the
    /// stepper's; what is left here is checking a call, matching a target with its channel's
    /// display, and the one LateUpdate. Tick is public so a test can drive a frame by hand.
    /// </summary>
    public class WorldPointerService : IWorldPointerService, IConstructable
    {
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [Inject] private IWorldPointerModel _model { get; set; }

        private readonly WorldPointerStepper _stepper = new();
        private Camera _camera;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        /// <summary>How many targets are registered, matched or waiting. For the tests; not on the interface.</summary>
        public int Count => _model.TargetCount;

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

        public void RegisterTarget(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrEmpty(channel) || target == null)
            {
                FlowLogger.LogError("RegisterTarget - a target needs a channel and a Transform, and one of them is missing. "
                                    + $"Registered from {file}:{line}.");
                return;
            }

            if (_model.TryGetTarget(channel, target, out _))
            {
                FlowLogger.LogError($"RegisterTarget - '{target.name}' is already registered on '{channel}'. Two pointers on one "
                                    + $"object use two channels. Registered from {file}:{line}.", target);
                return;
            }

            WorldPointerTargetRVO entry = _model.AddTarget(channel, target);

            if (entry.Owner.Slot != null)
                Match(entry);
        }

        public void UnregisterTarget(string channel, Transform target)
        {
            if (!_model.TryGetTarget(channel, target, out WorldPointerTargetRVO entry)) return;

            Unmatch(entry);
            _model.RemoveTarget(entry);
        }

        public void SetContent<TContent>(string channel, Transform target, TContent content,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            WorldPointerTargetRVO entry = FindForRequest(channel, target, "SetContent", file, line);
            if (entry == null) return;

            WorldPointerDisplaySlot slot = entry.Owner.Slot;

            if (slot != null && !slot.Accepts(content))
            {
                FlowLogger.LogError($"SetContent - the display of '{channel}' ({slot.Name}) shows {slot.ContentType.Name}, "
                                    + $"not {typeof(TContent).Name}. Sent from {file}:{line}.", target);
                return;
            }

            _model.SetContent(entry, content);

            if (entry.Indicator != null)
                slot.SetContent(entry.Indicator, content);
        }

        public void Show(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            WorldPointerTargetRVO entry = FindForRequest(channel, target, "Show", file, line);
            if (entry == null || entry.Visible) return;

            _model.SetVisible(entry, true);
        }

        public void Hide(string channel, Transform target,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            WorldPointerTargetRVO entry = FindForRequest(channel, target, "Hide", file, line);
            if (entry == null || !entry.Visible) return;

            _model.SetVisible(entry, false);
            if (entry.Indicator != null) Report(entry, WorldPointerState.Hidden);
        }

        public void UnregisterAll()
        {
            IReadOnlyList<WorldPointerChannelRVO> channels = _model.Channels;

            for (int c = 0; c < channels.Count; c++)
            {
                List<WorldPointerTargetRVO> members = channels[c].Members;

                for (int i = 0; i < members.Count; i++)
                    Unmatch(members[i]);
            }

            _model.ClearTargets();
        }

        // ---------------------------------------------------------------- the screen side

        public void RegisterDisplay<TContent>(string channel, IWorldPointerDisplay<TContent> display,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrEmpty(channel) || display == null || display is Object unityObject && unityObject == null)
            {
                FlowLogger.LogError("RegisterDisplay - a display needs a channel and a display, and one of them is missing. "
                                    + $"Registered from {file}:{line}.");
                return;
            }

            var slot = new WorldPointerDisplaySlot<TContent>(display);
            WorldPointerChannelRVO existing = _model.GetChannel(channel);

            if (existing?.Slot != null)
            {
                FlowLogger.LogError($"RegisterDisplay - '{channel}' is already drawn by {existing.Slot.Name}; {slot.Name} is refused. "
                                    + $"One channel has one display. Registered from {file}:{line}.", display as Object);
                return;
            }

            if (existing != null && HoldsOtherContent(existing, slot, file, line))
                return;

            List<WorldPointerTargetRVO> members = _model.SetDisplay(channel, slot).Members;

            // Backwards, so a target destroyed while it waited can leave in the same walk.
            for (int i = members.Count - 1; i >= 0; i--)
            {
                WorldPointerTargetRVO entry = members[i];

                if (entry.Target == null)
                    _model.RemoveTarget(entry);
                else
                    Match(entry);
            }
        }

        public void UnregisterDisplay(string channel)
        {
            WorldPointerChannelRVO row = _model.GetChannel(channel);
            if (row?.Slot == null) return;

            RemoveDisplay(row);
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
        /// One frame for every channel that has a display; a channel with none has nothing to move.
        /// A destroyed display is dropped and its targets wait. Both walks go backwards, so an entry
        /// can leave mid-walk: a destroyed target is unregistered and its indicator released; a
        /// destroyed indicator is let go and the target waits for the next display. No camera at
        /// all leaves every indicator Hidden until one turns up.
        /// </summary>
        public void Tick(float deltaTime)
        {
            Camera camera = Camera;
            IReadOnlyList<WorldPointerChannelRVO> channels = _model.Channels;

            for (int c = channels.Count - 1; c >= 0; c--)
            {
                WorldPointerChannelRVO channel = channels[c];
                if (channel.Slot == null) continue;

                if (channel.Slot.IsDestroyed)
                {
                    RemoveDisplay(channel);
                    continue;
                }

                List<WorldPointerTargetRVO> members = channel.Members;

                for (int i = members.Count - 1; i >= 0; i--)
                    Step(members[i], camera, deltaTime);
            }
        }

        private void LateUpdate() => Tick(Time.deltaTime);

        private void Step(WorldPointerTargetRVO entry, Camera camera, float deltaTime)
        {
            if (entry.Target == null)
            {
                Unmatch(entry);
                _model.RemoveTarget(entry);
                return;
            }

            if (entry.Indicator == null) return;

            if (entry.Indicator is Object indicatorObject && indicatorObject == null || entry.Indicator.Rect == null)
            {
                _model.SetIndicator(entry, null, null);
                return;
            }

            if (!entry.Visible) return;

            WorldPointerState state = camera == null
                ? WorldPointerState.Hidden
                : _stepper.Step(entry, entry.Owner.Slot.Options, camera, deltaTime);

            Report(entry, state);
        }

        /// <summary>Told on the first tick, then only on a change. A destroyed indicator is not told anything.</summary>
        private void Report(WorldPointerTargetRVO entry, WorldPointerState state)
        {
            if (!_model.SetState(entry, state)) return;
            if (entry.Indicator is Object unityObject && unityObject == null) return;

            entry.Indicator.SetState(state);
        }

        // ---------------------------------------------------------------- matching

        /// <summary>
        /// Takes an indicator from the channel's display for one target and replays what the target
        /// was told while it waited. An indicator the display could not give, or one with no Canvas
        /// above it, is reported against the display and handed back; the target keeps waiting.
        /// </summary>
        private void Match(WorldPointerTargetRVO entry)
        {
            WorldPointerDisplaySlot slot = entry.Owner.Slot;
            IWorldPointerIndicator indicator = slot.Acquire();

            if (indicator == null || indicator.Rect == null)
            {
                FlowLogger.LogError($"{slot.Name} gave no indicator with a RectTransform for '{entry.Target.name}' on "
                                    + $"'{entry.Owner.Id}', so nothing points at it.", slot.Display as Object);
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
            _model.SetIndicator(entry, indicator,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera);

            if (entry.HasContent)
                slot.SetContent(indicator, entry.Value);

            if (!entry.Visible)
                Report(entry, WorldPointerState.Hidden);
        }

        /// <summary>Hands the indicator back to its display. The display decides how it goes; nothing is told Hidden.</summary>
        private void Unmatch(WorldPointerTargetRVO entry)
        {
            IWorldPointerIndicator indicator = entry.Indicator;
            if (indicator == null) return;

            WorldPointerDisplaySlot slot = entry.Owner.Slot;
            _model.SetIndicator(entry, null, null);

            if (slot == null || slot.IsDestroyed) return;
            if (indicator is Object unityObject && unityObject == null) return;

            slot.Release(indicator);
        }

        private void RemoveDisplay(WorldPointerChannelRVO channel)
        {
            List<WorldPointerTargetRVO> members = channel.Members;

            for (int i = 0; i < members.Count; i++)
                Unmatch(members[i]);

            _model.SetDisplay(channel.Id, null);
        }

        private static bool HoldsOtherContent(WorldPointerChannelRVO channel, WorldPointerDisplaySlot slot, string file, int line)
        {
            foreach (WorldPointerTargetRVO entry in channel.Members)
            {
                if (!entry.HasContent || slot.Accepts(entry.Value)) continue;

                FlowLogger.LogError($"RegisterDisplay - {slot.Name} shows {slot.ContentType.Name}, but '{entry.Target.name}' "
                                    + $"on '{channel.Id}' holds {entry.Value.GetType().Name}. Registered from {file}:{line}.",
                    slot.Display as Object);
                return true;
            }

            return false;
        }

        private WorldPointerTargetRVO FindForRequest(string channel, Transform target, string request, string file, int line)
        {
            if (_model.TryGetTarget(channel, target, out WorldPointerTargetRVO entry)) return entry;

            string name = target != null ? target.name : "null";
            FlowLogger.LogError($"{request} - '{name}' is not registered on '{channel}'. RegisterTarget comes first. "
                                + $"Sent from {file}:{line}.", target);
            return null;
        }
    }
}