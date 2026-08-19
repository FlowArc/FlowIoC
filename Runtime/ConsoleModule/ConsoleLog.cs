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
    }
}