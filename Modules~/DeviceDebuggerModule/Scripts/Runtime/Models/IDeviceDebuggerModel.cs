using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.UnityObjects;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using Modules.DeviceDebuggerModule.Entities;
using Modules.DeviceDebuggerModule.Enums;

namespace Modules.DeviceDebuggerModule.Models
{
    /// <summary>
    /// The module's state: the config read off the Root's adapter, the log ring, whether the panel is open and on which tab, and what the last discovery found. The ring
    /// is fed by AddLog straight from FlowLogger's hook - the one path in the module that runs
    /// without a Command, because a Command per log line would log itself for ever.
    /// </summary>
    public interface IDeviceDebuggerModel
    {
        CD_DeviceDebugger Config { get; }

        LogRing Logs { get; }

        bool IsCapturing { get; }

        bool IsOpen { get; }

        /// <summary>Never Last; Console until a tab is chosen.</summary>
        DebugTab ActiveTab { get; }

        IReadOnlyList<DebugOptionVO> Options { get; }

        IReadOnlyList<DebugSignalVO> Signals { get; }

        /// <summary>Null until the assemblies were scanned once; the scan is kept for the Root's life.</summary>
        IReadOnlyList<Type> CommandOptionTypes { get; }

        IReadOnlyList<InfoRowVO> Info { get; }

        /// <summary>Moves on every SetOptions, so the view repaints only when discovery changed something.</summary>
        int OptionsVersion { get; }

        void AddLog(ConsoleLog log);

        void SetCapturing(bool on);

        void SetOpen(bool open);

        /// <summary>Last keeps the current tab.</summary>
        void SetActiveTab(DebugTab tab);

        void SetOptions(List<DebugOptionVO> options, List<DebugSignalVO> signals);

        void SetCommandOptionTypes(List<Type> types);

        void SetInfo(List<InfoRowVO> rows);

        /// <summary>
        /// The debugger's own signal for a step option, one per option key for the Root's life - a
        /// step annotated twice with different arguments gets two. Created says whether this call
        /// made it, which is when the caller binds it.
        /// </summary>
        Signal TriggerFor(string optionKey, string label, out bool created);
    }
}
