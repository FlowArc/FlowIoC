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
                // A step a Service ships is nested - Ns.IHapticService+Commands+Play - and written in
                // a file named the way its binding reads, IHapticService.Commands.Play.cs, so that
                // file is tried first. The outermost type's file is the fallback, for a nested type
                // declared inline.
                if (IsNested(blameTypeName))
                {
                    attempts.Add(new ScriptSearchAttempt
                    {
                        Kind = ScriptSearchKind.FileName, Value = NestedFileNameOf(blameTypeName), LineNumber = 0
                    });
                }

                attempts.Add(new ScriptSearchAttempt
                {
                    Kind = ScriptSearchKind.BlameTypeName, Value = OutermostTypeOf(blameTypeName), LineNumber = 0
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

        private bool IsNested(string typeName) => typeName.IndexOf('+') >= 0;

        /// <summary>
        /// Reflection spells a nested chain with '+' - <c>Modules.HapticModule.Services.IHapticService+Commands+Play</c>.
        /// The outermost type, <c>Modules.HapticModule.Services.IHapticService</c>, is what a file of
        /// the ordinary shape declares, so everything from the first '+' on is dropped.
        /// </summary>
        private string OutermostTypeOf(string typeName)
        {
            int nested = typeName.IndexOf('+');
            return nested < 0 ? typeName : typeName.Substring(0, nested);
        }

        /// <summary>
        /// The same chain as a file name: the namespace off, and the '+' as the '.' the binding is
        /// written with - <c>IHapticService.Commands.Play</c>.
        /// </summary>
        private string NestedFileNameOf(string typeName)
        {
            int nested = typeName.IndexOf('+');
            int lastDot = typeName.LastIndexOf('.', nested);
            string chain = lastDot < 0 ? typeName : typeName.Substring(lastDot + 1);
            return chain.Replace('+', '.');
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