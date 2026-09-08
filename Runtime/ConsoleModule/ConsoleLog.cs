using System;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// Serializable because the console has to survive a domain reload. Recompiling resets every
    /// static in the project, this list among them, and a console that empties itself whenever a
    /// script is saved is a console nobody can read a compile error in.
    /// </summary>
    [Serializable]
    public class ConsoleLog
    {
        public int Hour;
        public int Minute;
        public int Second;
        public int Millisecond;

        public SystemLogType SystemLogType;
        public int LogTypeValue;
        public LogType LogType;
        public string Message;

        public string SourceFilePath;
        public int SourceLineNumber;
        public string SourceClassName;
        public string SourceTrace;
        public string StackTrace;
        public Color LogColor = Color.white;

        /// <summary>Which door this log came in through.</summary>
        public LogSource Source;

        /// <summary>What the console's Collapse mode folds equal rows on.</summary>
        public int CollapseKey;

        /// <summary>Time.frameCount when the log was written.</summary>
        public int Frame;

        /// <summary>Time.realtimeSinceStartup when the log was written, for the delta column.</summary>
        public float Realtime;

        /// <summary>
        /// The full name of the type this diagnostic is about, when it knows. Double-clicking
        /// the log opens this type's script even when the object was never on the stack - an
        /// asynchronous release, or a resolver noticing a step later than it happened.
        /// </summary>
        public string BlameTypeName;

        /// <summary>The flow this log belongs to. 0 when it was written outside any flow.</summary>
        public int FlowId;

        /// <summary>The flow this one was started from. 0 at the root of a tree.</summary>
        public int ParentFlowId;
    }
}