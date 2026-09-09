#if UNITY_EDITOR
using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Shader errors on their way into Flow Console.
    ///
    /// They reach neither door <see cref="UnityLogBridge"/> holds open. A shader that will not
    /// compile is written from native straight into Unity's own console - its stack trace there is
    /// empty, which is the tell - and never passes through Application.logMessageReceived, so the
    /// window promising to be *the* console showed nothing at all. Measured 2026-09-09: two errors
    /// in Unity's console, ShaderUtil reporting a message count of two, and Flow Console recording
    /// neither, while a Debug.LogWarning in the same session landed normally.
    ///
    /// There is no callback for it, so the messages are read off the asset after an import. Unlike
    /// the compiler's, they need no parking in SessionState: importing a shader does not reload the
    /// domain, so there is nothing to survive.
    /// </summary>
    internal class ShaderLogBridge : AssetPostprocessor
    {
        /// <summary>
        /// Held here rather than beside the Unity bridge's own, and reachable from it, because the
        /// two have to agree about one thing: whether the shader error Unity is now printing to its
        /// console is one this bridge already put on the Shader channel with a file and a line.
        /// </summary>
        internal static readonly ShaderLogIntake Intake = new();

        /// <summary>
        /// Unity requires this entry point to be static, so it holds nothing and decides nothing -
        /// it hands the paths to the instance work below.
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < deletedAssets.Length; i++)
                Intake.Forget(deletedAssets[i]);

            for (int i = 0; i < movedFromAssetPaths.Length; i++)
                Intake.Forget(movedFromAssetPaths[i]);

            Report(importedAssets);
            Report(movedAssets);
        }

        private static void Report(string[] assetPaths)
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string path = assetPaths[i];

                if (!Intake.IsShaderAsset(path)) continue;

                ReportOne(path);
            }
        }

        private static void ReportOne(string assetPath)
        {
            ShaderMessage[] messages = MessagesFor(assetPath);
            int count = messages == null ? 0 : messages.Length;

            var lines = new List<string>(count);
            string shaderName = ShaderNameOf(assetPath);

            for (int i = 0; i < count; i++)
            {
                ShaderMessage message = messages[i];

                lines.Add(Intake.Describe(shaderName, message.message, message.platform.ToString()));
            }

            // Asked even when there is nothing to say, and that is the point: a shader that
            // compiles now has to be forgotten, or breaking it again later with the error it had
            // before would be read as a repeat of a row that is no longer on screen and never
            // reported at all.
            if (!Intake.ShouldReport(assetPath, lines)) return;

            for (int i = 0; i < count; i++)
            {
                ShaderMessage message = messages[i];

                // The file and the line ride structurally rather than in the text, so the row can
                // be double-clicked open the way a compiler row can. A message that names no file
                // falls back to the asset, which is always somewhere worth opening.
                string file = string.IsNullOrEmpty(message.file) ? assetPath : message.file;

                // Remembered before it is written, so Unity's own copy of this error - which
                // arrives later, when the variant compiles, and carries no file - is recognised
                // and dropped rather than shown beside this one.
                Intake.Remember(message.message);

                FlowLogger.AddExternalLog(LogSource.Shader,
                    message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error
                        ? LogType.Error
                        : LogType.Warning,
                    lines[i], Intake.Detail(message.messageDetails), file, message.line);
            }
        }

        /// <summary>
        /// The three kinds of shader asset carry their messages behind three different calls, and
        /// asking the wrong one of a given asset returns nothing rather than throwing. The type
        /// loaded from the path is what decides which to ask.
        /// </summary>
        private static ShaderMessage[] MessagesFor(string assetPath)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            switch (asset)
            {
                case Shader shader:
                    return ShaderUtil.GetShaderMessageCount(shader) == 0
                        ? null
                        : ShaderUtil.GetShaderMessages(shader);

                case ComputeShader compute:
                    return ShaderUtil.GetComputeShaderMessageCount(compute) == 0
                        ? null
                        : ShaderUtil.GetComputeShaderMessages(compute);

                case RayTracingShader rayTracing:
                    return ShaderUtil.GetRayTracingShaderMessageCount(rayTracing) == 0
                        ? null
                        : ShaderUtil.GetRayTracingShaderMessages(rayTracing);

                default:
                    return null;
            }
        }

        private static string ShaderNameOf(string assetPath)
        {
            var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;

            return shader == null ? null : shader.name;
        }
    }
}
#endif