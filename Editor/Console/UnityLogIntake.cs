#if UNITY_EDITOR
using System.Text.RegularExpressions;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The decisions the Unity bridge makes about an inbound message, kept apart from the hooks
    /// themselves so they can be tested without an editor callback to fire.
    /// </summary>
    public class UnityLogIntake
    {
        private readonly ExternalLogEchoPolicy _echoPolicy = new();

        public bool ShouldRecord(LogType logType, bool weAreWritingToUnityConsole)
        {
            return !_echoPolicy.IsEcho(weAreWritingToUnityConsole);
        }

        /// <summary>
        /// Whether the message is a compiler diagnostic Unity is printing to its own console.
        ///
        /// The same error arrives twice: once through CompilationPipeline, carrying the file and
        /// the line, and once as text through Application.logMessageReceived, carrying neither. So
        /// the console showed one compile error as two rows, and the row on the Unity channel had
        /// nowhere to go when it was double-clicked. This is the one that is dropped.
        /// </summary>
        public bool IsCompilerMessage(string message)
        {
            return message != null && CompilerMessage.IsMatch(message);
        }

        /// <summary>
        /// What Unity prints for a compile error: the file, the line and column in brackets, then
        /// error or warning, then the compiler's own code.
        /// </summary>
        private static readonly Regex CompilerMessage =
            new(@"\(\d+,\d+\):\s*(error|warning)\s+\w+\d+:", RegexOptions.Compiled);

        /// <summary>
        /// Whether the message is a shader diagnostic Unity is printing to its own console.
        ///
        /// The same error arrives twice here as well: once off the asset at import, carrying the
        /// file and the line, and once as this text when the variant actually compiles, carrying
        /// neither. Saying yes is only half the answer - the caller drops it only if the shader
        /// bridge really did record that error, because a variant that fails at play time was
        /// never imported and this text is then the only copy there is.
        /// </summary>
        public bool IsShaderMessage(string message)
        {
            return message != null && ShaderMessage.IsMatch(message);
        }

        /// <summary>
        /// What Unity prints for a shader diagnostic: the word Shader, error or warning, then the
        /// shader's name in quotes.
        /// </summary>
        private static readonly Regex ShaderMessage =
            new(@"^Shader\s+(error|warning)\s+in\s+'", RegexOptions.Compiled);

        /// <summary>
        /// Whether the message is Unity's own forwarding of a player's line. The receiver behind
        /// Unity's Console re-logs one as the player's kind and name in italics, then the text,
        /// with no stack trace of its own. While a FlowIoC player is attached that row has already
        /// arrived over our message with its channel and flow, so this copy is dropped; when no
        /// such player is attached it is the only copy and is kept.
        /// </summary>
        public bool IsPlayerEcho(string message, string stackTrace, bool flowPlayerPresent)
        {
            if (!flowPlayerPresent) return false;
            if (!string.IsNullOrEmpty(stackTrace)) return false;

            return message != null && PlayerEcho.IsMatch(message);
        }

        /// <summary>What Unity's receiver writes: <i>PlayerType "name"</i>, a space, the line.</summary>
        private static readonly Regex PlayerEcho = new(@"^<i>[^<]*""</i> ", RegexOptions.Compiled);

        /// <summary>
        /// Folds Exception and Assert onto Error. The console offers three filters, so a kind
        /// outside them would answer to none and could neither be hidden nor found.
        /// </summary>
        public LogType ToLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.Exception:
                case LogType.Assert:
                    return LogType.Error;
                default:
                    return logType;
            }
        }
    }
}
#endif