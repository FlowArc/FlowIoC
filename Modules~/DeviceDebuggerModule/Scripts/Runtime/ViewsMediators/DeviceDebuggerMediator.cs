using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using FlowIoC.ConsoleModule;
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
    /// badge, the stats as often as the config says, a Value row when its signal fires. A Mediator
    /// injects its View and signals and nothing else, which is why the model's state arrives on
    /// a signal, asked for once when the view registers.
    /// </summary>
    public class DeviceDebuggerMediator : IMediator
    {
        [Inject] private DeviceDebuggerView _view { get; set; }
        [InjectSignal] private DeviceDebuggerInternalSignals _signals { get; set; }

        // Built once the config is known, because its window is the config's.
        private StatsSampler _sampler;
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

            if (sampling) _sampler?.Tick(unscaledDeltaTime);

            if (!_state.IsOpen)
            {
                if (_state.Logs.UnreadErrors != _paintedBadge)
                {
                    _paintedBadge = _state.Logs.UnreadErrors;
                    _view.SetErrorBadge(_paintedBadge);
                }

                if (!_state.Config.ShowFpsOnTrigger) return;

                _triggerFpsClock += unscaledDeltaTime;

                if (_triggerFpsClock < _state.Config.TriggerFpsRefreshSeconds) return;

                _triggerFpsClock = 0f;
                _view.SetTriggerFps(_sampler?.Fps ?? 0f, true);
                return;
            }

            switch (_state.ActiveTab)
            {
                case DebugTab.Console:
                    if (_state.Logs.Version != _paintedLogVersion) PaintLogs();
                    break;
                case DebugTab.Stats:
                    _statsClock += unscaledDeltaTime;
                    if (_statsClock < _state.Config.StatsRefreshSeconds) break;
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
            _sampler = new StatsSampler(_state.Config.StatsWindow);
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
            _statsClock = _state.Config.StatsRefreshSeconds;
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
            if (_sampler == null) return;

            StatsSampleVO sample = _sampler.Sample(
                Profiler.GetTotalAllocatedMemoryLong,
                Profiler.GetTotalReservedMemoryLong,
                () => GC.GetTotalMemory(false),
                () => GC.CollectionCount(0),
                Time.realtimeSinceStartup,
                Time.timeScale,
                TargetFrameMs());

            _view.PaintStats(sample);
        }

        /// <summary>
        /// The frame the game aims for, which is what the graph's guide is drawn at: the rate
        /// ConfigureFrameRateCommand set, or sixty when the game left it to the platform.
        /// </summary>
        private static float TargetFrameMs()
        {
            int rate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            return 1000f / rate;
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

        private void ValueArrived(DebugOptionVO row, object[] args) => _view.ShowOptionValue(row.Key, args);
    }
}