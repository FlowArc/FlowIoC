using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Constants;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;
using Modules.DeviceDebuggerModule.Signals;
using UnityEngine;
using UnityEngine.Profiling;

namespace Modules.DeviceDebuggerModule.ViewsMediators
{
    /// <summary>
    /// Drives the one view. Everything that decides something is dispatched - opening, closing,
    /// a tab (a Show with the tab, so discovery runs again), an option, a signal, a clear, a
    /// copy - and everything that only shows something is done here on the view's tick from the
    /// last PanelStateVO the module announced: the log list when the ring's version moved, the
    /// badge, the stats four times a second, a Value row when its signal fires. A Mediator
    /// injects its View and signals and nothing else, which is why the model's state arrives on
    /// a signal, asked for once when the view registers.
    /// </summary>
    public class DeviceDebuggerMediator : IMediator
    {
        private const float STATS_INTERVAL = 0.25f;
        private const float TRIGGER_FPS_INTERVAL = 1f;

        [Inject] private DeviceDebuggerView _view { get; set; }
        [InjectSignal] private DeviceDebuggerInternalSignals _signals { get; set; }

        private readonly StatsSampler _sampler = new();
        private readonly List<ConsoleLog> _visible = new();
        private readonly Dictionary<DebugOptionVO, Action<object[]>> _valueListeners = new();

        private PanelStateVO _state;
        private bool _configured;
        private int _paintedLogVersion = -1;
        private int _paintedOptionsVersion = -1;
        private int _paintedBadge = -1;
        private float _statsClock;
        private float _triggerFpsClock;

        public void OnRegister()
        {
            _view.OnTriggerTapped += TriggerTapped;
            _view.OnCloseTapped += CloseTapped;
            _view.OnTabSelected += TabSelected;
            _view.OnOptionActivated += OptionActivated;
            _view.OnSignalFired += SignalFired;
            _view.OnFilterChanged += FilterChanged;
            _view.OnClearTapped += ClearTapped;
            _view.OnCopyLogsTapped += CopyLogsTapped;
            _view.OnCopyDetailTapped += CopyDetailTapped;
            _view.OnCopyInfoTapped += CopyInfoTapped;
            _view.OnLogSelected += LogSelected;
            _view.OnTick += Tick;

            _signals.PanelStateChanged.AddListener(PanelStateChanged);

            // The view may register before or after the module's Initialize; asking makes both fine.
            _signals.RequestPanelState.Dispatch();
        }

        public void OnRemove()
        {
            _view.OnTriggerTapped -= TriggerTapped;
            _view.OnCloseTapped -= CloseTapped;
            _view.OnTabSelected -= TabSelected;
            _view.OnOptionActivated -= OptionActivated;
            _view.OnSignalFired -= SignalFired;
            _view.OnFilterChanged -= FilterChanged;
            _view.OnClearTapped -= ClearTapped;
            _view.OnCopyLogsTapped -= CopyLogsTapped;
            _view.OnCopyDetailTapped -= CopyDetailTapped;
            _view.OnCopyInfoTapped -= CopyInfoTapped;
            _view.OnLogSelected -= LogSelected;
            _view.OnTick -= Tick;

            _signals.PanelStateChanged.RemoveListener(PanelStateChanged);

            StopValueListeners();
            _state = null;
            _configured = false;
        }

        // ======================== What the tester did ========================

        private void TriggerTapped() => _signals.Toggle.Dispatch();

        private void CloseTapped() => _signals.Hide.Dispatch();

        private void TabSelected(DebugTab tab) => _signals.Show.Dispatch(tab);

        private void OptionActivated(DebugOptionVO option, string text) => _signals.ActivateOption.Dispatch(option, text);

        private void SignalFired(DebugSignalVO row, string text) => _signals.FireSignal.Dispatch(row, text);

        private void ClearTapped() => _signals.ClearLogs.Dispatch();

        private void CopyLogsTapped() => _signals.CopyLogs.Dispatch();

        private void CopyDetailTapped(ConsoleLog row) => _signals.CopyLog.Dispatch(row);

        private void CopyInfoTapped() => _signals.CopyInfo.Dispatch();

        private void FilterChanged()
        {
            _paintedLogVersion = -1;
            _view.ClearLogSelection();
        }

        private void LogSelected(ConsoleLog row) => _view.PaintDetail(row);

        // ======================== What the module announced ========================

        private void PanelStateChanged(PanelStateVO state)
        {
            bool wasOpen = _state != null && _state.IsOpen;
            _state = state;

            if (!_configured)
            {
                Configure();
                return;
            }

            if (state.OptionsVersion != _paintedOptionsVersion) PaintDiscovered();

            if (state.IsOpen == wasOpen)
            {
                if (state.IsOpen) ShowActiveTab();
                return;
            }

            _view.ShowPanel(state.IsOpen);

            if (state.IsOpen) ShowActiveTab();
            else StopValueListeners();
        }

        // ======================== The tick ========================

        private void Tick(float unscaledDeltaTime)
        {
            if (_state == null) return;

            if (!_configured)
            {
                Configure();
                if (!_configured) return;
            }

            bool sampling = _state.IsOpen || _state.Config.ShowFpsOnTrigger;

            if (sampling) _sampler.Tick(unscaledDeltaTime);

            if (!_state.IsOpen)
            {
                if (_state.Logs.UnreadErrors != _paintedBadge)
                {
                    _paintedBadge = _state.Logs.UnreadErrors;
                    _view.SetErrorBadge(_paintedBadge);
                }

                if (!_state.Config.ShowFpsOnTrigger) return;

                _triggerFpsClock += unscaledDeltaTime;

                if (_triggerFpsClock < TRIGGER_FPS_INTERVAL) return;

                _triggerFpsClock = 0f;
                _view.SetTriggerFps(_sampler.Fps, true);
                return;
            }

            switch (_state.ActiveTab)
            {
                case DebugTab.Console:
                    if (_state.Logs.Version != _paintedLogVersion) PaintLogs();
                    break;
                case DebugTab.Stats:
                    _statsClock += unscaledDeltaTime;
                    if (_statsClock < STATS_INTERVAL) break;
                    _statsClock = 0f;
                    PaintStats();
                    break;
            }
        }

        // ======================== Painting ========================

        private void Configure()
        {
            if (_state == null || !_view.IsBuilt) return;

            _view.ApplyConfig(_state.Config);
            _view.SetTriggerFps(0f, false);
            _view.ShowPanel(_state.IsOpen);
            _view.SetErrorBadge(_state.Logs.UnreadErrors);
            _paintedBadge = _state.Logs.UnreadErrors;
            _configured = true;

            PaintDiscovered();

            if (_state.IsOpen) ShowActiveTab();
        }

        private void ShowActiveTab()
        {
            _view.ShowTab(_state.ActiveTab);
            _view.SetErrorBadge(_state.Logs.UnreadErrors);
            _paintedBadge = _state.Logs.UnreadErrors;
            _paintedLogVersion = -1;
            _statsClock = STATS_INTERVAL;
            PaintForTab();
        }

        private void PaintDiscovered()
        {
            _paintedOptionsVersion = _state.OptionsVersion;
            _view.PaintOptions(_state.Options);
            _view.PaintSignals(_state.Signals);
            _view.PaintInfo(_state.Info);
            _view.PaintChannels(_state.Channels);
            StartValueListeners();
        }

        private void PaintForTab()
        {
            switch (_state.ActiveTab)
            {
                case DebugTab.Console:
                    PaintLogs();
                    break;
                case DebugTab.Stats:
                    PaintStats();
                    break;
                case DebugTab.Info:
                    _view.PaintInfo(_state.Info);
                    break;
            }
        }

        private void PaintLogs()
        {
            _paintedLogVersion = _state.Logs.Version;
            _state.Logs.CopyTo(_visible, _view.Filter);
            _view.PaintLogs(_visible, true);
            _view.PaintCounts(_state.Logs.Logs, _state.Logs.Warnings, _state.Logs.Errors);
        }

        private void PaintStats()
        {
            StatsSampleVO sample = _sampler.Sample(
                Profiler.GetTotalAllocatedMemoryLong,
                Profiler.GetTotalReservedMemoryLong,
                () => GC.GetTotalMemory(false),
                () => GC.CollectionCount(0),
                Time.realtimeSinceStartup,
                Time.timeScale);

            _view.PaintStats(sample);
        }

        // ======================== Value rows ========================

        private void StartValueListeners()
        {
            StopValueListeners();

            if (_state?.Options == null) return;

            foreach (DebugOptionVO option in _state.Options)
            {
                if (option.Kind != DebugOptionKind.Value || option.Signal == null) continue;

                DebugOptionVO row = option;
                Action<object[]> listener = args => ValueArrived(row, args);
                row.Signal.AddListenerUntyped(listener);
                _valueListeners[row] = listener;
            }
        }

        private void StopValueListeners()
        {
            foreach (KeyValuePair<DebugOptionVO, Action<object[]>> pair in _valueListeners)
                pair.Key.Signal?.RemoveListenerUntyped(pair.Value);

            _valueListeners.Clear();
        }

        private void ValueArrived(DebugOptionVO row, object[] args)
        {
            row.Dispatches++;
            row.LastValue = args == null || args.Length == 0
                ? "× " + row.Dispatches + ", last " + DateTime.Now.ToString("HH:mm:ss")
                : Join(args);

            _view.RefreshOptionValue(row);

            if (_state?.Options == null) return;

            // A control that shares the row's key shows what the module just announced.
            foreach (DebugOptionVO option in _state.Options)
            {
                if (option == row || option.Key != row.Key || option.Kind == DebugOptionKind.Value) continue;

                option.LastValue = row.LastValue;
                _view.RefreshOptionValue(option);
            }
        }

        private static string Join(object[] args)
        {
            var parts = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
                parts[i] = args[i] == null ? DeviceDebuggerConstants.NO_VALUE : args[i].ToString();

            return string.Join(", ", parts);
        }
    }
}