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

        /// <summary>
        /// The channel this log is on, by name. A name rather than a number because a project's
        /// channels are one per module and there is nobody to hand out numbers: two developers
        /// adding a module on two branches are handed the same one, and a number reassigned when
        /// the list is sorted moves every row already recorded onto somebody else's channel. The
        /// framework's own channels are named for their SystemLogType.
        /// </summary>
        public string Channel;

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

        /// <summary>
        /// Whether the reader asked to keep this one. A pinned log is passed over by the trim that
        /// bounds the list, and can be shown on its own. The mark lives here rather than in the
        /// window so that it survives a domain reload with the log it belongs to.
        /// </summary>
        public bool Pinned;

        /// <summary>Time.frameCount when the log was written.</summary>
        public int Frame;

        /// <summary>Time.realtimeSinceStartup when the log was written, for the delta column.</summary>
        public float Realtime;

        /// <summary>
        /// Whether the game was running when this was written. The console draws a line wherever
        /// this changes, so a list that spans a play session says where the session began and
        /// ended rather than running the two together.
        /// </summary>
        public bool InPlayMode;

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