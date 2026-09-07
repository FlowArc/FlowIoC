#if UNITY_EDITOR
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The decisions the Unity bridge makes about an inbound message, kept apart from the hooks
    /// themselves so they can be tested without an editor callback to fire.
    /// </summary>
    public class UnityLogIntake
    {
        private readonly ExternalLogEchoPolicy _echoPolicy = new();

        public bool ShouldRecord(LogType logType, bool weAreWritingToUnityConsole)
        {
            return !_echoPolicy.IsEcho(weAreWritingToUnityConsole);
        }

        /// <summary>
        /// Folds Exception and Assert onto Error. The console offers three filters, so a kind
        /// outside them would answer to none and could neither be hidden nor found.
        /// </summary>
        public LogType ToLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Exception:
                case LogType.Assert:
                    return LogType.Error;
                default:
                    return logType;
            }
        }
    }
}
#endif
