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