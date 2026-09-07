#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.ConsoleModule;

namespace FlowIoC.Editor.Console
{
    public enum ScriptSearchKind
    {
        DirectPath = 0,
        RelativePath = 1,
        BlameTypeName = 2,
        FileName = 3
    }

    public struct ScriptSearchAttempt
    {
        public ScriptSearchKind Kind;
        public string Value;
        public int LineNumber;
    }

    /// <summary>
    /// What to try, and in what order, when working out which script a log points at. Kept
    /// apart from AssetDatabase so the ordering can be tested: a captured path is the best
    /// answer because it carries a line number, and the blamed type is the answer when the
    /// offending object was never on the stack.
    /// </summary>
    public class ScriptAssetSearchPlan
    {
        private static readonly FlowStackFrameFilter PathText = new();

        public List<ScriptSearchAttempt> Build(string blameTypeName, string sourceFilePath, int sourceLineNumber)
        {
            var attempts = new List<ScriptSearchAttempt>();

            bool hasPath = !string.IsNullOrEmpty(sourceFilePath);
            string normalized = hasPath ? sourceFilePath.Replace('\\', '/') : null;

            if (hasPath)
            {
                attempts.Add(new ScriptSearchAttempt
                {
                    Kind = ScriptSearchKind.DirectPath, Value = normalized, LineNumber = sourceLineNumber
                });

                string relative = ToProjectRelative(normalized);
                if (relative != null)
                {
                    attempts.Add(new ScriptSearchAttempt
                    {
                        Kind = ScriptSearchKind.RelativePath, Value = relative, LineNumber = sourceLineNumber
                    });
                }
            }

            if (!string.IsNullOrEmpty(blameTypeName))
            {
                attempts.Add(new ScriptSearchAttempt
                {
                    Kind = ScriptSearchKind.BlameTypeName, Value = blameTypeName, LineNumber = 0
                });
            }

            if (hasPath)
            {
                // Taken apart by hand: a path read out of a stack trace can hold characters
                // System.IO.Path refuses, and it throws rather than answering.
                string fileName = PathText.FileNameWithoutExtensionOf(normalized);
                if (!string.IsNullOrEmpty(fileName))
                {
                    attempts.Add(new ScriptSearchAttempt
                    {
                        Kind = ScriptSearchKind.FileName, Value = fileName, LineNumber = sourceLineNumber
                    });
                }
            }

            return attempts;
        }

        private static string ToProjectRelative(string normalized)
        {
            int assets = normalized.IndexOf("/Assets/", StringComparison.Ordinal);
            if (assets >= 0) return normalized.Substring(assets + 1);

            int packages = normalized.IndexOf("/Packages/", StringComparison.Ordinal);
            if (packages >= 0) return normalized.Substring(packages + 1);

            return null;
        }
    }
}
#endif