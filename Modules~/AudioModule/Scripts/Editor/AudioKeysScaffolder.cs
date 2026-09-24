#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using Modules.AudioModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.AudioModule.Editor
{
    /// <summary>
    /// Gives a module what it needs to play sound, all of it inside that module: a part of
    /// AudioKey for its keys with the asmref that compiles it into Modules.Audio.Shared, a bank
    /// at Resources/Audio/CD_AudioBank.asset, and the two references its assembly needs. Nothing is
    /// written into the Audio module, so an update of Audio never meets any of it. What already
    /// exists is left as it is, so running it twice changes nothing.
    /// </summary>
    internal class AudioKeysScaffolder
    {
        internal const string KEYS_FOLDER = "Scripts/AudioKeys";
        internal const string ASMREF_FILE = "Modules.Audio.Shared.asmref";
        internal const string BANK_FOLDER = "Resources/Audio";
        internal const string BANK_FILE = "CD_AudioBank.asset";
        internal const string SHARED_ASSEMBLY = "Modules.Audio.Shared";
        internal const string SERVICE_ASSEMBLY = "Modules.Audio";

        private const string AUDIO_MODULE = "AudioModule";
        private const string MODULE_SUFFIX = "Module";

        /// <summary>The nested class a module's keys sit in: GameplayModule answers Gameplay.</summary>
        internal string ClassNameOf(string moduleName) =>
            moduleName.EndsWith(MODULE_SUFFIX) && moduleName.Length > MODULE_SUFFIX.Length
                ? moduleName.Substring(0, moduleName.Length - MODULE_SUFFIX.Length)
                : moduleName;

        internal string KeyFileOf(string moduleFolder) =>
            Combine(moduleFolder, KEYS_FOLDER, $"AudioKey.{ClassNameOf(Path.GetFileName(moduleFolder))}.cs");

        internal string BankPathOf(string moduleFolder) => Combine(moduleFolder, BANK_FOLDER, BANK_FILE);

        internal bool HasKeys(string moduleFolder) => File.Exists(Combine(moduleFolder, KEYS_FOLDER, ASMREF_FILE));

        internal bool HasBank(string moduleFolder) => File.Exists(BankPathOf(moduleFolder));

        /// <summary>Whether a module has both halves, one of them, or neither.</summary>
        internal AudioKeysStatus StatusOf(string moduleFolder)
        {
            bool keys = HasKeys(moduleFolder);
            bool bank = HasBank(moduleFolder);

            if (keys && bank)
                return AudioKeysStatus.Installed;

            return keys || bank ? AudioKeysStatus.Partial : AudioKeysStatus.None;
        }

        /// <summary>
        /// How deep a module sits in the tree: 0 for one straight under Assets/Modules, one more for
        /// every zSubModules, zScreenModules or zTestModules on the way down to it.
        /// </summary>
        internal int DepthOf(string moduleFolder)
        {
            int depth = 0;

            foreach (string part in moduleFolder.Replace('\\', '/').Split('/'))
            {
                if (part == "zSubModules" || part == "zScreenModules" || part == "zTestModules")
                    depth++;
            }

            return depth;
        }

        /// <summary>
        /// Every module folder in the project - a folder that holds its own Modules.*.asmdef and is
        /// named ...Module - as a project path, the Audio module itself left out.
        /// </summary>
        internal List<string> FindModuleFolders()
        {
            var folders = new List<string>();
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            foreach (string asmdef in Directory.GetFiles(Application.dataPath, "Modules.*.asmdef", SearchOption.AllDirectories))
            {
                string folder = Path.GetDirectoryName(asmdef);
                string name = Path.GetFileName(folder);

                if (name == null || !name.EndsWith(MODULE_SUFFIX) || name == AUDIO_MODULE)
                    continue;

                folders.Add(Path.GetRelativePath(projectRoot, folder).Replace('\\', '/'));
            }

            folders.Sort();
            return folders;
        }

        /// <summary>Writes whatever of the four the module is missing and refreshes the project.</summary>
        internal void Scaffold(string moduleFolder)
        {
            string moduleName = Path.GetFileName(moduleFolder);
            string keysFolder = Combine(moduleFolder, KEYS_FOLDER);
            Directory.CreateDirectory(keysFolder);

            string asmref = Combine(keysFolder, ASMREF_FILE);

            if (!File.Exists(asmref))
                File.WriteAllText(asmref, "{\n    \"reference\": \"" + SHARED_ASSEMBLY + "\"\n}\n");

            string keyFile = KeyFileOf(moduleFolder);

            if (!File.Exists(keyFile))
                File.WriteAllText(keyFile, KeyFileText(moduleName, moduleFolder.Replace('\\', '/').Contains("/zTestModules/")));

            string asmdef = FindAsmdef(moduleFolder);

            if (asmdef != null)
                File.WriteAllText(asmdef, WithReferences(File.ReadAllText(asmdef), SHARED_ASSEMBLY, SERVICE_ASSEMBLY));

            AssetDatabase.Refresh();

            string bankPath = BankPathOf(moduleFolder);

            if (!File.Exists(bankPath))
            {
                Directory.CreateDirectory(Combine(moduleFolder, BANK_FOLDER));
                AssetDatabase.Refresh();

                var bank = ScriptableObject.CreateInstance<CD_AudioBank>();
                bank.Module = moduleName;
                AssetDatabase.CreateAsset(bank, bankPath);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>The asmdef text with the references added that it does not already carry.</summary>
        internal string WithReferences(string asmdefText, params string[] references)
        {
            const string key = "\"references\"";
            int keyAt = asmdefText.IndexOf(key, System.StringComparison.Ordinal);

            if (keyAt < 0)
                return asmdefText;

            int open = asmdefText.IndexOf('[', keyAt);
            int close = asmdefText.IndexOf(']', open);

            if (open < 0 || close < 0)
                return asmdefText;

            string list = asmdefText.Substring(open + 1, close - open - 1);
            var builder = new StringBuilder(list.TrimEnd());

            foreach (string reference in references)
            {
                if (list.Contains("\"" + reference + "\""))
                    continue;

                if (builder.ToString().Trim().Length > 0)
                    builder.Append(',');

                builder.Append("\n    \"").Append(reference).Append('"');
            }

            return asmdefText.Substring(0, open + 1) + builder + "\n  " + asmdefText.Substring(close);
        }

        private static string FindAsmdef(string moduleFolder)
        {
            string[] asmdefs = Directory.GetFiles(moduleFolder, "*.asmdef", SearchOption.TopDirectoryOnly);
            return asmdefs.Length > 0 ? asmdefs[0] : null;
        }

        private string KeyFileText(string moduleName, bool testModule)
        {
            string className = ClassNameOf(moduleName);
            var text = new StringBuilder();

            if (testModule)
                text.Append("#if UNITY_EDITOR\n\n");

            text.Append("// ReSharper disable once CheckNamespace\n");
            text.Append("namespace Modules.AudioModule.Shared\n{\n");
            text.Append("    /// <summary>\n");
            text.Append($"    /// The sounds {moduleName} plays. This file lives in {moduleName} and is compiled into\n");
            text.Append("    /// Modules.Audio.Shared by the asmref beside it, so its keys join every other module's under\n");
            text.Append($"    /// AudioKey. Each id starts with \"{moduleName}/\" - the bank the sound loads from, which is\n");
            text.Append($"    /// {moduleName}/{BANK_FOLDER}/{BANK_FILE}.\n");
            text.Append("    /// </summary>\n");
            text.Append("    public readonly partial struct AudioKey\n    {\n");
            text.Append($"        public static class {className}\n        {{\n");
            text.Append($"            // public static readonly AudioKey Jump = new(\"{moduleName}/Jump\");\n");
            text.Append("        }\n    }\n}\n");

            if (testModule)
                text.Append("\n#endif\n");

            return text.ToString();
        }

        private static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
    }
}

#endif