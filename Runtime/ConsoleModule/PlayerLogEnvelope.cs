using System;
using System.Text;
using UnityEngine;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// One message over the player connection, both ways of reading it. JSON through JsonUtility
    /// rather than a hand-written binary format, because the two ends can be on different versions
    /// of the package: a field one side does not know is skipped and one it did not receive keeps
    /// its default, where a binary layout would read garbage from the first change. It is what the
    /// console already uses to carry rows across a domain reload.
    /// </summary>
    [Serializable]
    public class PlayerLogEnvelope
    {
        /// <summary>
        /// The message id FlowIoC's rows travel under. Unity's own log forwarding has ids of its
        /// own; this one is ours, so the editor bridge is handed nothing but what the sender wrote.
        /// </summary>
        public static readonly Guid MessageId = new("7f1c4b2e-6d3a-4a9e-9b1e-3c5f0a8d2e41");

        public PlayerLogKind Kind;
        public string Player;
        public ConsoleLog Log;

        public static PlayerLogEnvelope Hello(string player)
        {
            return new PlayerLogEnvelope {Kind = PlayerLogKind.Hello, Player = player};
        }

        public static PlayerLogEnvelope ForLog(ConsoleLog log)
        {
            return new PlayerLogEnvelope {Kind = PlayerLogKind.Log, Log = log};
        }

        public byte[] Write()
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
        }

        /// <summary>Null when the bytes are not an envelope; the bridge records nothing then.</summary>
        public static PlayerLogEnvelope Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                return JsonUtility.FromJson<PlayerLogEnvelope>(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
