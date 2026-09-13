using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The one hook on Unity's log stream, for the editor's bridge and the player's sender alike.
    /// Application.logMessageReceived fires for the main thread only: a Debug.Log from a Task or
    /// a thread never reaches it, and the line Unity's own console shows would be missing from
    /// Flow Console. This listens to the threaded event instead, which fires on every thread, and
    /// carries a line written off the main thread back onto it - through the synchronization
    /// context it is given, or a Drain the owner calls from its own update. What travels with the
    /// line is whether it was FlowLogger's own write coming back, read on the thread that wrote
    /// it, because that flag is raised only around the write itself.
    /// </summary>
    public class UnityLogRelay
    {
        public delegate void LineHandler(string condition, string stackTrace, LogType type, bool isEcho);

        private struct Line
        {
            public string Condition;
            public string StackTrace;
            public LogType Type;
            public bool IsEcho;
        }

        private readonly LineHandler _onMainThread;
        private readonly Func<bool> _isEchoNow;
        private readonly SynchronizationContext _mainThread;
        private readonly int _mainThreadId;
        private readonly ConcurrentQueue<Line> _waiting = new();

        /// <summary>Built on the main thread, which is how it knows that thread again.</summary>
        public UnityLogRelay(LineHandler onMainThread, Func<bool> isEchoNow, SynchronizationContext mainThread = null)
        {
            _onMainThread = onMainThread;
            _isEchoNow = isEchoNow;
            _mainThread = mainThread;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void Hook()
        {
            Application.logMessageReceivedThreaded -= Receive;
            Application.logMessageReceivedThreaded += Receive;
        }

        public void Unhook()
        {
            Application.logMessageReceivedThreaded -= Receive;
        }

        /// <summary>A line as Unity hands it over, on whichever thread wrote it.</summary>
        public void Receive(string condition, string stackTrace, LogType type)
        {
            bool isEcho = _isEchoNow();

            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                _onMainThread(condition, stackTrace, type, isEcho);
                return;
            }

            _waiting.Enqueue(new Line {Condition = condition, StackTrace = stackTrace, Type = type, IsEcho = isEcho});
            _mainThread?.Post(_ => Drain(), null);
        }

        /// <summary>Hands over every line written on another thread since the last call. Called on the main thread.</summary>
        public void Drain()
        {
            while (_waiting.TryDequeue(out Line line))
                _onMainThread(line.Condition, line.StackTrace, line.Type, line.IsEcho);
        }
    }
}
