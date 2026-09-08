using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FlowIoC.BaseModule.Signals;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Connectors
{
    /// <summary>
    /// Static helper class that manages signal connections between modules
    /// </summary>
    public static class SignalConnector
    {
        private static readonly Dictionary<string, List<Action>> _disconnectActionsById = new ();
        private static readonly Dictionary<ISignalBody, List<Action>> _disconnectActionsBySignal = new ();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DisconnectAll();

        #region Connect Methods - No Parameters

        /// <summary>
        /// Connects two parameterless signals
        /// </summary>
        public static void Connect(this ISignal source, ISignal target, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action callback = () =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch();
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a parameterless signal to an action
        /// </summary>
        public static void Connect(this ISignal source, Action callback, string groupId = null)
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        #endregion

        #region Connect Methods - 1 Parameter

        /// <summary>
        /// Connects two signals with the same parameter type
        /// </summary>
        public static void Connect<T>(this ISignal<T> source, ISignal<T> target, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T> callback = param =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(param);
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal to an action with one parameter
        /// </summary>
        public static void Connect<T>(this ISignal<T> source, Action<T> callback, string groupId = null)
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Connects two signals with different parameter types using a converter
        /// </summary>
        public static void Connect<T1, TResult>(this ISignal<T1> source, ISignal<TResult> target, Func<T1, TResult> converter,
            string groupId = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1> callback = sourceParam =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(converter(sourceParam));
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 2 Parameters

        /// <summary>
        /// Connects two signals with two parameters
        /// </summary>
        public static void Connect<T1, T2>(this ISignal<T1, T2> source, ISignal<T1, T2> target, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2> callback = (param1, param2) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(param1, param2);
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with two parameters to an action
        /// </summary>
        public static void Connect<T1, T2>(this ISignal<T1, T2> source, Action<T1, T2> callback, string groupId = null)
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a two-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, TResult>(this ISignal<T1, T2> source, ISignal<TResult> target,
            Func<T1, T2, TResult> converter, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2> callback = (param1, param2) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(converter(param1, param2));
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 3 Parameters

        /// <summary>
        /// Connects two signals with three parameters
        /// </summary>
        public static void Connect<T1, T2, T3>(this ISignal<T1, T2, T3> source, ISignal<T1, T2, T3> target,
            string groupId = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2, T3> callback = (param1, param2, param3) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(param1, param2, param3);
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with three parameters to an action
        /// </summary>
        public static void Connect<T1, T2, T3>(this ISignal<T1, T2, T3> source, Action<T1, T2, T3> callback, string groupId = null)
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a three-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, T3, TResult>(this ISignal<T1, T2, T3> source, ISignal<TResult> target,
            Func<T1, T2, T3, TResult> converter, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2, T3> callback = (param1, param2, param3) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(converter(param1, param2, param3));
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 4 Parameters

        /// <summary>
        /// Connects two signals with four parameters
        /// </summary>
        public static void Connect<T1, T2, T3, T4>(this ISignal<T1, T2, T3, T4> source, ISignal<T1, T2, T3, T4> target,
            string groupId = null, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2, T3, T4> callback = (param1, param2, param3, param4) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(param1, param2, param3, param4);
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with four parameters to an action
        /// </summary>
        public static void Connect<T1, T2, T3, T4>(this ISignal<T1, T2, T3, T4> source, Action<T1, T2, T3, T4> callback, string groupId = null)
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a four-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, T3, T4, TResult>(this ISignal<T1, T2, T3, T4> source, ISignal<TResult> target,
            Func<T1, T2, T3, T4, TResult> converter, string groupId = null,
            [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        {
            Action<T1, T2, T3, T4> callback = (param1, param2, param3, param4) =>
            {
                string previousFile = null;
                int previousLine = 0;
                BeginCrossing(source, target, file, line, ref previousFile, ref previousLine);

                try
                {
                    target.Dispatch(converter(param1, param2, param3, param4));
                }
                finally
                {
                    FlowLogger.ExitDeclaration(previousFile, previousLine);
                }
            };
            source.Connect(callback, groupId);
        }

        #endregion

        #region Disconnect Methods

        public static void Disconnect(this ISignalBody source, string groupId = null)
        {
            if (groupId == null)
            {
                
                if (!_disconnectActionsBySignal.ContainsKey(source))
                    return;

                foreach (var action in _disconnectActionsBySignal[source])
                {
                    action?.Invoke();
                }

                _disconnectActionsBySignal.Remove(source); 
            }
            else
            {
                DisconnectGroup(groupId);
            }
        }

        /// <summary>
        /// Removes all connections in a specific group
        /// </summary>
        public static void DisconnectGroup(string groupId)
        {
            if (!_disconnectActionsById.ContainsKey(groupId))
                return;

            foreach (var action in _disconnectActionsById[groupId])
            {
                action?.Invoke();
            }

            _disconnectActionsById.Remove(groupId);
        }

        /// <summary>
        /// Removes all connections
        /// </summary>
        public static void DisconnectAll()
        {
            foreach (var actionsById in _disconnectActionsById.Values)
            {
                foreach (var action in actionsById)
                {
                    action?.Invoke();
                }
            }
            foreach (var actionsBySignal in _disconnectActionsBySignal.Values)
            {
                foreach (var action in actionsBySignal)
                {
                    action?.Invoke();
                }
            }
            _disconnectActionsById.Clear();
            _disconnectActionsBySignal.Clear();
        }

        #endregion

        /// <summary>
        /// The line a Connector writes when it carries one module's announcement to another's order.
        /// Without it a crossing is invisible: the target simply dispatches, and nothing in the
        /// console says which Connector decided that, or where to read the wiring.
        ///
        /// The file and line are the ones the compiler wrote into the Connect call, so the row
        /// opens the Connector's own Setup rather than the module that happened to be on the stack.
        /// </summary>
        private static void BeginCrossing(ISignalBody source, ISignalBody target, string file, int line,
            ref string previousFile, ref int previousLine)
        {
            FlowLogger.LogAt(SystemLogType.Signal, file, line,
                "[Connector] '", source.Name, "' to '", target.Name + "'.");

            // The dispatch that follows was decided here, so it points here too. Left alone it
            // named whatever sequence the announcement came out of, which is the module on the
            // other side of the crossing - true of the announcement, and misleading about the order.
            FlowLogger.EnterDeclaration(file, line, ref previousFile, ref previousLine);
        }

        private static void RegisterDisconnector(ISignalBody signal, string groupId, Action disconnectAction)
        {

            if (groupId == null)
            {
                if (!_disconnectActionsBySignal.ContainsKey(signal))
                {
                    _disconnectActionsBySignal[signal] = new List<Action>();
                }
                _disconnectActionsBySignal[signal].Add(disconnectAction);
            }
            else
            {
                if (!_disconnectActionsById.ContainsKey(groupId))
                {
                    _disconnectActionsById[groupId] = new List<Action>();
                }
                _disconnectActionsById[groupId].Add(disconnectAction);
            }
        }

    }
} 