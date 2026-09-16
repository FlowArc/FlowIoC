using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.UnityObjects;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;
using Modules.DeviceDebuggerModule.RootsContexts;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Models
{
    /// <summary>
    /// PostConstruct only takes the config off the Root's adapter - the shipped defaults when the
    /// adapter carries none, because a panel with default settings beats no panel - and sizes the
    /// ring by it. Everything else is a plain Set or a plain read.
    /// </summary>
    public class DeviceDebuggerModel : IDeviceDebuggerModel, IConstructable
    {
        [Inject(nameof(DeviceDebuggerServiceContext))]
        private GameObject _root { get; set; }

        private readonly List<DebugOptionVO> _options = new();
        private readonly List<DebugSignalVO> _signals = new();
        private readonly List<InfoRowVO> _info = new();
        private readonly Dictionary<string, Signal> _triggers = new();
        private List<Type> _commandOptionTypes;

        public bool IsPostConstructed { get; set; }

        public bool IsDeconstructed { get; set; }

        public CD_DeviceDebugger Config { get; private set; }

        public LogRing Logs { get; private set; }

        public bool IsCapturing { get; private set; }

        public bool IsOpen { get; private set; }

        public DebugTab ActiveTab { get; private set; } = DebugTab.Console;

        public IReadOnlyList<DebugOptionVO> Options => _options;

        public IReadOnlyList<DebugSignalVO> Signals => _signals;

        public IReadOnlyList<Type> CommandOptionTypes => _commandOptionTypes;

        public IReadOnlyList<InfoRowVO> Info => _info;

        public int OptionsVersion { get; private set; }

        public DeviceDebuggerModel()
        {
            // Usable before PostConstruct, so a test or an early caller never meets a null ring.
            Config = ScriptableObject.CreateInstance<CD_DeviceDebugger>();
            Logs = new LogRing(Config.LogCapacity);
        }

        public void PostConstruct()
        {
            if (!TryReadAdapter(out CD_DeviceDebugger config)) return;

            Config = config;
            Logs = new LogRing(Config.LogCapacity);
        }

        public void AddLog(ConsoleLog log) => Logs.Add(log);

        public void SetCapturing(bool on) => IsCapturing = on;

        public void SetOpen(bool open) => IsOpen = open;

        public void SetActiveTab(DebugTab tab)
        {
            if (tab == DebugTab.Last) return;

            ActiveTab = tab;
        }

        public void SetOptions(List<DebugOptionVO> options, List<DebugSignalVO> signals)
        {
            _options.Clear();
            _signals.Clear();

            if (options != null) _options.AddRange(options);
            if (signals != null) _signals.AddRange(signals);

            OptionsVersion++;
        }

        public void SetCommandOptionTypes(List<Type> types) => _commandOptionTypes = types ?? new List<Type>();

        public void SetInfo(List<InfoRowVO> rows)
        {
            _info.Clear();

            if (rows != null) _info.AddRange(rows);
        }

        public Signal TriggerFor(string optionKey, string label, out bool created)
        {
            created = false;

            if (_triggers.TryGetValue(optionKey ?? "", out Signal trigger)) return trigger;

            trigger = new Signal(name: label);
            _triggers[optionKey ?? ""] = trigger;
            created = true;

            return trigger;
        }

        private bool TryReadAdapter(out CD_DeviceDebugger config)
        {
            config = null;

            RootAdapter adapter = _root != null ? _root.GetComponent<RootAdapter>() : null;

            if (adapter == null)
            {
                FlowLogger.LogWarning("DeviceDebuggerServiceRoot has no RootAdapter, so the panel runs on its default settings.");
                return false;
            }

            config = adapter.GetScriptable<CD_DeviceDebugger>();

            if (config == null)
                FlowLogger.LogWarning("DeviceDebuggerServiceRoot's adapter carries no CD_DeviceDebugger, so the panel runs on its default settings.");

            return config != null;
        }
    }
}
