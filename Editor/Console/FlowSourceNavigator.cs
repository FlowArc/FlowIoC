#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.ConsoleModule;
using UnityEditor;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// Opens the script a log points at. There is one of these because there used to be two
    /// near-identical methods on the console window, each re-deriving the same fallbacks.
    ///
    /// The order comes from ScriptAssetSearchPlan: a captured path first, because it carries a
    /// line number, then the blamed type, which is all there is when the offending object was
    /// never on the stack.
    /// </summary>
    public class FlowSourceNavigator
    {
        private readonly ScriptAssetSearchPlan _plan = new();

        public bool Open(ConsoleLog log)
        {
            if (log == null) return false;

            // The blamed type is preferred over the captured class name: a diagnostic that named
            // who it is about knows better than a stack frame does.
            string blame = !string.IsNullOrEmpty(log.BlameTypeName) ? log.BlameTypeName : log.SourceClassName;

            return Open(log.SourceFilePath, log.SourceLineNumber, blame);
        }

        public bool Open(string filePath, int lineNumber, string blameTypeName)
        {
            List<ScriptSearchAttempt> attempts = _plan.Build(blameTypeName, filePath, lineNumber);

            for (int i = 0; i < attempts.Count; i++)
            {
                if (TryOpen(attempts[i])) return true;
            }

            return false;
        }

        private bool TryOpen(ScriptSearchAttempt attempt)
        {
            switch (attempt.Kind)
            {
                case ScriptSearchKind.DirectPath:
                case ScriptSearchKind.RelativePath:
                    return OpenAtPath(attempt.Value, attempt.LineNumber);

                case ScriptSearchKind.BlameTypeName:
                    return OpenByTypeName(attempt.Value, attempt.LineNumber);

                case ScriptSearchKind.FileName:
                    return OpenByFileName(attempt.Value, attempt.LineNumber);

                default:
                    return false;
            }
        }

        private static bool OpenAtPath(string assetPath, int lineNumber)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null) return false;

            AssetDatabase.OpenAsset(asset, lineNumber);
            return true;
        }

        /// <summary>
        /// A full type name narrows two files of the same name down to one, by looking for the
        /// namespace inside the candidate before opening it.
        /// </summary>
        private static bool OpenByTypeName(string fullTypeName, int lineNumber)
        {
            string[] parts = fullTypeName.Split('.');
            string simpleName = parts[parts.Length - 1];
            string namespaceName = parts.Length > 1
                ? string.Join(".", parts, 0, parts.Length - 1)
                : null;

            string[] guids = AssetDatabase.FindAssets(simpleName + " t:Script");

            if (namespaceName != null)
            {
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(assetPath) != simpleName) continue;

                    try
                    {
                        string content = File.ReadAllText(assetPath);
                        bool declaresType = content.Contains("class " + simpleName)
                                            || content.Contains("struct " + simpleName);

                        if (declaresType && content.Contains("namespace " + namespaceName))
                            return OpenAtPath(assetPath, lineNumber);
                    }
                    catch (Exception)
                    {
                        // An unreadable candidate is simply not the answer; keep looking.
                    }
                }
            }

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath) == simpleName)
                    return OpenAtPath(assetPath, lineNumber);
            }

            return false;
        }

        private static bool OpenByFileName(string fileName, int lineNumber)
        {
            string[] guids = AssetDatabase.FindAssets(fileName + " t:Script");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath) == fileName)
                    return OpenAtPath(assetPath, lineNumber);
            }

            return false;
        }
    }
}
#endif
