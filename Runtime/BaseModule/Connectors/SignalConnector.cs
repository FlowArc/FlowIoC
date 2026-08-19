using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Signals;

namespace FlowIoC.BaseModule.Connectors
{
    /// <summary>
    /// Static helper class that manages signal connections between modules
    /// </summary>
    public static class SignalConnector
    {
        [ShowInModelViewer]
        private static readonly Dictionary<string, List<Action>> _disconnectActionsById = new ();
        private static readonly Dictionary<ISignalBody, List<Action>> _disconnectActionsBySignal = new ();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DisconnectAll();

        #region Connect Methods - No Parameters

        /// <summary>
        /// Connects two parameterless signals
        /// </summary>
        public static void Connect(this ISignal source, ISignal target, string groupId = "signalName")
        {
            Action callback = () => target.Dispatch();
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a parameterless signal to an action
        /// </summary>
        public static void Connect(this ISignal source, Action callback, string groupId = "signalName")
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        #endregion

        #region Connect Methods - 1 Parameter

        /// <summary>
        /// Connects two signals with the same parameter type
        /// </summary>
        public static void Connect<T>(this ISignal<T> source, ISignal<T> target, string groupId = "signalName")
        {
            Action<T> callback = param => target.Dispatch(param);
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal to an action with one parameter
        /// </summary>
        public static void Connect<T>(this ISignal<T> source, Action<T> callback, string groupId = "signalName")
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Connects two signals with different parameter types using a converter
        /// </summary>
        public static void Connect<T1, TResult>(this ISignal<T1> source, ISignal<TResult> target, Func<T1, TResult> converter, string groupId = "signalName")
        {
            Action<T1> callback = sourceParam => target.Dispatch(converter(sourceParam));
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 2 Parameters

        /// <summary>
        /// Connects two signals with two parameters
        /// </summary>
        public static void Connect<T1, T2>(this ISignal<T1, T2> source, ISignal<T1, T2> target, string groupId = "signalName")
        {
            Action<T1, T2> callback = (param1, param2) => target.Dispatch(param1, param2);
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with two parameters to an action
        /// </summary>
        public static void Connect<T1, T2>(this ISignal<T1, T2> source, Action<T1, T2> callback, string groupId = "signalName")
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a two-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, TResult>(this ISignal<T1, T2> source, ISignal<TResult> target, Func<T1, T2, TResult> converter, string groupId = "signalName")
        {
            Action<T1, T2> callback = (param1, param2) => target.Dispatch(converter(param1, param2));
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 3 Parameters

        /// <summary>
        /// Connects two signals with three parameters
        /// </summary>
        public static void Connect<T1, T2, T3>(this ISignal<T1, T2, T3> source, ISignal<T1, T2, T3> target, string groupId = "signalName")
        {
            Action<T1, T2, T3> callback = (param1, param2, param3) => target.Dispatch(param1, param2, param3);
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with three parameters to an action
        /// </summary>
        public static void Connect<T1, T2, T3>(this ISignal<T1, T2, T3> source, Action<T1, T2, T3> callback, string groupId = "signalName")
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a three-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, T3, TResult>(this ISignal<T1, T2, T3> source, ISignal<TResult> target, Func<T1, T2, T3, TResult> converter, string groupId = "signalName")
        {
            Action<T1, T2, T3> callback = (param1, param2, param3) => target.Dispatch(converter(param1, param2, param3));
            source.Connect(callback, groupId);
        }

        #endregion

        #region Connect Methods - 4 Parameters

        /// <summary>
        /// Connects two signals with four parameters
        /// </summary>
        public static void Connect<T1, T2, T3, T4>(this ISignal<T1, T2, T3, T4> source, ISignal<T1, T2, T3, T4> target, string groupId = "signalName")
        {
            Action<T1, T2, T3, T4> callback = (param1, param2, param3, param4) => target.Dispatch(param1, param2, param3, param4);
            source.Connect(callback, groupId);
        }

        /// <summary>
        /// Connects a signal with four parameters to an action
        /// </summary>
        public static void Connect<T1, T2, T3, T4>(this ISignal<T1, T2, T3, T4> source, Action<T1, T2, T3, T4> callback, string groupId = "signalName")
        {
            source.AddListener(callback);
            RegisterDisconnector(source, groupId, () => source.RemoveListener(callback));
        }

        /// <summary>
        /// Converts a four-parameter signal to a single-parameter signal
        /// </summary>
        public static void Connect<T1, T2, T3, T4, TResult>(this ISignal<T1, T2, T3, T4> source, ISignal<TResult> target, Func<T1, T2, T3, T4, TResult> converter, string groupId = "signalName")
        {
            Action<T1, T2, T3, T4> callback = (param1, param2, param3, param4) => target.Dispatch(converter(param1, param2, param3, param4));
            source.Connect(callback, groupId);
        }

        #endregion

        #region Disconnect Methods

        public static void Disconnect(this SignalBody source, string groupId = "signalName")
        {
            if (groupId == "signalName")
            {
                //     groupId = ((ISignalBody) source).Name;
                
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

        private static void RegisterDisconnector(ISignalBody signal, string groupId, Action disconnectAction)
        {

            if (groupId == "signalName")
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