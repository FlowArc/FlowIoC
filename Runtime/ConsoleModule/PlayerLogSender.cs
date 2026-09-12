using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace FlowIoC.ConsoleModule
{
    /// <summary>
    /// The player's end of the Flow Console. Every row FlowLogger would have recorded, and every
    /// line the player's Unity writes, goes over the player connection to whichever editor is
    /// attached - the one Unity's own Console and the Profiler attach through. The class compiles
    /// everywhere so the editor checks it; only a development player installs it, and only a
    /// connected one pays for a row.
    /// </summary>
    public class PlayerLogSender
    {
        private readonly ExternalLogEchoPolicy _echoPolicy = new();

        public bool IsConnected => PlayerConnection.instance.isConnected;

        /// <summary>
        /// TrySend rather than Send: a buffer the editor has not drained drops the row instead of
        /// stalling the game, and a console is not worth a hitch.
        /// </summary>
        public void Send(ConsoleLog log)
        {
            if (log == null) return;

            PlayerConnection.instance.TrySend(PlayerLogEnvelope.MessageId, PlayerLogEnvelope.ForLog(log).Write());
        }

        public void SendHello()
        {
            string name = Application.platform + " " + SystemInfo.deviceName;

            PlayerConnection.instance.TrySend(PlayerLogEnvelope.MessageId, PlayerLogEnvelope.Hello(name).Write());
        }

        /// <summary>
        /// Hooks the connection and the player's own log stream. Called once, from the engine's
        /// entry point below; autoconnect can beat the hook, so a connection already up says hello
        /// straight away.
        /// </summary>
        public void Install()
        {
            PlayerConnection.instance.RegisterConnection(OnConnected);

            Application.logMessageReceived -= OnUnityLog;
            Application.logMessageReceived += OnUnityLog;

            if (IsConnected) SendHello();
        }

        private void OnConnected(int playerId)
        {
            SendHello();
        }

        /// <summary>
        /// The player's Unity lines - an exception, a native warning, a third party's Debug.Log.
        /// FlowLogger's own lines come back through here too, and are skipped the way the editor
        /// bridge skips them, so a row travels once.
        /// </summary>
        private void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (!IsConnected) return;
            if (_echoPolicy.IsEcho(FlowLogger.IsWritingToUnityConsole)) return;

            FlowLogger.AddExternalLog(LogSource.Unity, type, condition, stackTrace, null, 0);
        }

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        /// <summary>
        /// One of the statics the engine forces. A release player never runs this, so it carries
        /// nothing; the editor has its own bridge and is not a player.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallInPlayer()
        {
            FlowLogger.Sender.Install();
        }
#endif
    }
}
