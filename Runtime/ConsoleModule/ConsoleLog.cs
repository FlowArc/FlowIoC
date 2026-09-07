using UnityEngine;

namespace FlowIoC.ConsoleModule
{
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
    }
}