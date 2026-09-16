#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// A player's rows on their way into Flow Console. Hooked at editor load like the Unity
    /// bridge, on the same player connection Unity's Console and the Profiler attach through, but
    /// under FlowIoC's own message id: what arrives is what PlayerLogSender wrote, with the
    /// channel, the flow and the source the device had - not the text Unity's forwarding flattens
    /// it to.
    ///
    /// What the intake learns from a Hello is parked in SessionState and read back here, because
    /// a recompile empties every static while the player stays connected and never says hello
    /// again - and without it the rows after the reload carried the connection's name and Unity's
    /// echo of them was no longer dropped.
    ///
    /// The handlers are taken off the connection just before the domain unloads, not just before
    /// they go on: EditorConnection logs "MessageHandler not registered" for an Unregister with
    /// nothing to take off, and the first launch of a session has nothing - so the old order put
    /// one red line in every fresh project's console before it had written a line of its own.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayerLogBridge
    {
        private const string INTAKE_KEY = "FlowIoC.Console.PlayerLogIntake";

        private static readonly PlayerLogIntake Intake = new();

        public static bool IsFlowPlayerPresent => Intake.IsFlowPlayerPresent;

        static PlayerLogBridge()
        {
            EditorConnection.instance.Initialize();

            EditorConnection.instance.Register(PlayerLogEnvelope.MessageId, OnMessage);
            EditorConnection.instance.RegisterDisconnection(OnDisconnected);

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            Intake.Restore(SessionState.GetString(INTAKE_KEY, null), ConnectedPlayerIds());
        }

        /// <summary>
        /// The domain that registered the handlers is the one that takes them off, so the next
        /// domain registers onto a clean connection and never has to guess whether the last one
        /// left anything behind.
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            EditorConnection.instance.Unregister(PlayerLogEnvelope.MessageId, OnMessage);
            EditorConnection.instance.UnregisterDisconnection(OnDisconnected);
        }

        private static void OnMessage(MessageEventArgs args)
        {
            ConsoleLog log = Intake.Receive(args.playerId, args.data, ConnectionNameOf(args.playerId));

            if (log != null)
                FlowLogger.AddPlayerLog(log);
            else
                Park();
        }

        private static void OnDisconnected(int playerId)
        {
            Intake.Disconnected(playerId);
            Park();
        }

        private static void Park()
        {
            SessionState.SetString(INTAKE_KEY, Intake.Serialize());
        }

        private static string ConnectionNameOf(int playerId)
        {
            List<ConnectedPlayer> players = EditorConnection.instance.ConnectedPlayers;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerId == playerId) return players[i].name;
            }

            return null;
        }

        private static IEnumerable<int> ConnectedPlayerIds()
        {
            List<ConnectedPlayer> players = EditorConnection.instance.ConnectedPlayers;

            for (int i = 0; i < players.Count; i++)
                yield return players[i].playerId;
        }
    }
}
#endif