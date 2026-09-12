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
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayerLogBridge
    {
        private static readonly PlayerLogIntake Intake = new();

        public static bool IsFlowPlayerPresent => Intake.IsFlowPlayerPresent;

        static PlayerLogBridge()
        {
            EditorConnection.instance.Initialize();

            EditorConnection.instance.Unregister(PlayerLogEnvelope.MessageId, OnMessage);
            EditorConnection.instance.Register(PlayerLogEnvelope.MessageId, OnMessage);

            EditorConnection.instance.UnregisterDisconnection(OnDisconnected);
            EditorConnection.instance.RegisterDisconnection(OnDisconnected);
        }

        private static void OnMessage(MessageEventArgs args)
        {
            ConsoleLog log = Intake.Receive(args.playerId, args.data, ConnectionNameOf(args.playerId));
            if (log != null) FlowLogger.AddPlayerLog(log);
        }

        private static void OnDisconnected(int playerId)
        {
            Intake.Disconnected(playerId);
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
    }
}
#endif
