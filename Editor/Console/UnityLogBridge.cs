#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Everything Unity writes, on its way into Flow Console. Hooked at editor load rather than
    /// when the window opens, so a log written before anybody opened it is still recorded.
    ///
    /// Nothing here reflects into UnityEditor.LogEntries. Compiler messages come from
    /// CompilationPipeline, which is public and has not moved, and the rest come from
    /// Application.logMessageReceived. An internal API that breaks silently on a Unity upgrade
    /// would be worse than a feature never promised.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityLogBridge
    {
        private const string PENDING_COMPILER_LOGS_KEY = "FlowIoC.Console.PendingCompilerLogs";

        private static readonly UnityLogIntake Intake = new();
        private static readonly CompilerLogStore Store = new();

        static UnityLogBridge()
        {
            Application.logMessageReceived -= OnUnityLog;
            Application.logMessageReceived += OnUnityLog;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompiled;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;

            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            DrainPendingCompilerLogs();
        }

        /// <summary>
        /// Each compile starts its round clean. Without this, messages parked by a compile that
        /// failed - and so was never followed by a domain reload to drain them - would be drained
        /// after the *next* compile and report errors the developer has already fixed.
        /// </summary>
        private static void OnCompilationStarted(object context)
        {
            SessionState.EraseString(PENDING_COMPILER_LOGS_KEY);
        }

        private static void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!Intake.ShouldRecord(type, FlowLogger.IsWritingToUnityConsole)) return;

            FlowLogger.AddExternalLog(LogSource.Unity, Intake.ToLogType(type), condition,
                stackTrace, null, 0);
        }

        /// <summary>
        /// Fires while the compile is running, and a domain reload follows it, so the messages
        /// are parked in SessionState and read back on the other side.
        /// </summary>
        private static void OnAssemblyCompiled(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages == null || messages.Length == 0) return;

            List<CompilerLogRecord> pending =
                Store.Deserialize(SessionState.GetString(PENDING_COMPILER_LOGS_KEY, null));

            for (int i = 0; i < messages.Length; i++)
            {
                CompilerMessage message = messages[i];

                if (message.type != CompilerMessageType.Error && message.type != CompilerMessageType.Warning)
                    continue;

                var record = new CompilerLogRecord
                {
                    Message = message.message,
                    File = message.file,
                    Line = message.line,
                    IsError = message.type == CompilerMessageType.Error
                };

                pending.Add(record);

                // Recorded now as well as parked. A compile that fails is not followed by a
                // domain reload, so the parked copy would never be drained - and a failed
                // compile is exactly when somebody needs to read the error.
                Record(record);
            }

            SessionState.SetString(PENDING_COMPILER_LOGS_KEY, Store.Serialize(pending));
        }

        private static void Record(CompilerLogRecord record)
        {
            FlowLogger.AddExternalLog(LogSource.Compiler,
                record.IsError ? LogType.Error : LogType.Warning,
                record.Message, null, record.File, record.Line);
        }

        private static void DrainPendingCompilerLogs()
        {
            List<CompilerLogRecord> pending =
                Store.Deserialize(SessionState.GetString(PENDING_COMPILER_LOGS_KEY, null));

            if (pending.Count == 0) return;

            SessionState.EraseString(PENDING_COMPILER_LOGS_KEY);

            for (int i = 0; i < pending.Count; i++)
                Record(pending[i]);
        }
    }
}
#endif