#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The decisions the player bridge makes about a message off the connection, kept apart from
    /// the hooks so they can be tested without a device: what a Hello means, which name a row is
    /// stamped with, and where a device's flow ids go so they never fold into the editor's.
    /// </summary>
    public class PlayerLogIntake
    {
        /// <summary>
        /// Both ends count flows from one. A device's ids are moved up here so Flow mode never
        /// groups its flow 4 under the editor's flow 4.
        /// </summary>
        public const int FlowIdOffset = 1_000_000_000;

        private readonly Dictionary<int, string> _names = new();
        private readonly HashSet<int> _present = new();

        /// <summary>
        /// Whether a player running FlowIoC is attached. While one is, the Unity bridge drops the
        /// echo Unity's own log forwarding makes of the same rows.
        /// </summary>
        public bool IsFlowPlayerPresent => _present.Count > 0;

        public string NameOf(int playerId)
        {
            return _names.TryGetValue(playerId, out string name) ? name : "Player " + playerId;
        }

        /// <summary>
        /// The row to record, or null when the message was a Hello or was not an envelope at all.
        /// connectionName is what the editor's connection calls the player, used for a row that
        /// arrives before the player said hello.
        /// </summary>
        public ConsoleLog Receive(int playerId, byte[] data, string connectionName)
        {
            PlayerLogEnvelope envelope = PlayerLogEnvelope.Read(data);
            if (envelope == null) return null;

            if (envelope.Kind == PlayerLogKind.Hello)
            {
                _present.Add(playerId);

                if (!string.IsNullOrEmpty(envelope.Player))
                    _names[playerId] = envelope.Player;
                else if (!string.IsNullOrEmpty(connectionName))
                    _names[playerId] = connectionName;

                return null;
            }

            ConsoleLog log = envelope.Log;
            if (log == null) return null;

            if (!_names.ContainsKey(playerId) && !string.IsNullOrEmpty(connectionName))
                _names[playerId] = connectionName;

            log.Player = NameOf(playerId);

            if (log.FlowId != 0) log.FlowId += FlowIdOffset;
            if (log.ParentFlowId != 0) log.ParentFlowId += FlowIdOffset;

            return log;
        }

        public void Disconnected(int playerId)
        {
            _present.Remove(playerId);
            _names.Remove(playerId);
        }
    }
}
#endif
