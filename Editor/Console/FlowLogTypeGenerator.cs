#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.ProjectPaths;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Migration;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>One module's channel as its generated part declares it: the name, the colour, and what decorates a line.</summary>
    internal class ChannelPartEVO
    {
        internal string Name { get; set; }
        internal Color32 Color { get; set; }
        internal FlowLogProfile Profile { get; set; }
    }

    internal static class FlowLogTypeGenerator
    {
        private static readonly FlowIoCProjectPaths Paths = new FlowIoCProjectPaths();

        private static readonly string GeneratedFolder = Paths.GeneratedRoot;
        private static readonly string GeneratedFilePath = Paths.FlowLogType;
        private static readonly string AsmRefPath = Paths.GeneratedAsmRef;

        private const string DEFAULT_CHANNEL = "Default";

        /// <summary>
        /// What puts a generated file into FlowIoC's own assembly rather than into whatever asmdef
        /// sits above it. The parts of FlowLogType have to share an assembly to be one class, and
        /// they are written into modules that each have an assembly of their own.
        /// </summary>
        private const string ASM_REF_CONTENT = "{\n    \"reference\": \"FlowIoC\"\n}";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += Generate;
        }

        /// <summary>
        /// Writes every part from what the project is: one per module in the index, and the
        /// shared file that carries the Default channel. There is no list of channels to consult -
        /// a module has a channel because it is a module - so a run reads the index and the cards
        /// and writes whatever differs from what is on disk.
        /// </summary>
        public static void Generate()
        {
            // Before anything is written at the new path. A copy of FlowLogType at the old path and
            // one at the new path at the same time is a duplicate type definition, not clutter.
            new FlowIoCPathMigrator().MigrateIfNeeded();

            EnsureDirectoryExists();
            EnsureAsmRefExists();

            int moduleCount = 0;
            bool wroteSomething = WriteModuleParts(ref moduleCount);

            string content = GenerateClassContent();
            string fullPath = GetFullPath(GeneratedFilePath);

            bool centralChanged = !File.Exists(fullPath) || File.ReadAllText(fullPath) != content;

            if (centralChanged)
            {
                File.WriteAllText(fullPath, content);
                AssetDatabase.ImportAsset(GeneratedFilePath, ImportAssetOptions.ForceUpdate);
            }

            if (!centralChanged && !wroteSomething)
            {
                Debug.Log($"<color=cyan>FlowConsole:</color> FlowLogType is already up to date ({moduleCount} module channel(s)).");
                return;
            }

            Debug.Log($"<color=cyan>FlowConsole:</color> FlowLogType generated with {moduleCount} module channel(s).");
        }

        /// <summary>
        /// A module's channel is declared in the module, in a part of its own. FlowLogType used to
        /// be one file listing every channel in the project, which made two people adding a module
        /// on two branches conflict over the same lines, and left a deleted module's channel behind
        /// for somebody to notice. A part per module has neither problem: the file is written into
        /// the module, it goes when the module goes, and nobody else's module touches it.
        ///
        /// The part carries an asmref beside it so that it compiles into FlowIoC rather than into
        /// the module's own assembly. Partial parts must share an assembly, and every module has an
        /// assembly of its own - so without the asmref this could not be a partial class at all.
        ///
        /// The colour beside the name is the module's own: what its card says, or the palette's
        /// pick for the name when the card says nothing. A test module has no channel.
        /// </summary>
        private static bool WriteModuleParts(ref int moduleCount)
        {
            ED_ModuleIndex index = new ModuleIndexProvider().LoadOrCreate();
            if (index == null) return false;

            bool wrote = false;
            var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reader = new ChannelPartReader();

            foreach (ModuleDescriptorEVO module in index.Modules
                         .Where(m => m.Kind != ModuleKind.Test)
                         .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                string moduleFolder = AssetDatabase.GUIDToAssetPath(module.FolderGuid);
                if (string.IsNullOrEmpty(moduleFolder)) continue;

                moduleCount++;

                string folder = moduleFolder + "/Scripts/Generated";
                string filePath = folder + "/FlowLogType." + module.Name + ".cs";

                written.Add(filePath);

                string content = GeneratePartContent(reader.Read(module.Name, GetFullPath(moduleFolder)));
                string fullPath = GetFullPath(filePath);

                if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content)
                {
                    EnsureModuleAsmRef(folder);
                    continue;
                }

                Directory.CreateDirectory(GetFullPath(folder));
                File.WriteAllText(fullPath, content);
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceUpdate);
                EnsureModuleAsmRef(folder);
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                wrote = true;
            }

            wrote |= RemoveOrphanParts(index, written);

            return wrote;
        }

        /// <summary>
        /// A part whose channel is gone: a module renamed while its old part stayed in the folder.
        /// Delete Module takes the whole module folder, so this is for the other way round.
        /// </summary>
        private static bool RemoveOrphanParts(ED_ModuleIndex index, HashSet<string> written)
        {
            bool removed = false;

            foreach (ModuleDescriptorEVO module in index.Modules)
            {
                string moduleFolder = AssetDatabase.GUIDToAssetPath(module.FolderGuid);
                if (string.IsNullOrEmpty(moduleFolder)) continue;

                string folder = moduleFolder + "/Scripts/Generated";
                string folderFullPath = GetFullPath(folder);
                if (!Directory.Exists(folderFullPath)) continue;

                foreach (string file in Directory.GetFiles(folderFullPath, "FlowLogType.*.cs"))
                {
                    string assetPath = folder + "/" + Path.GetFileName(file);
                    if (written.Contains(assetPath)) continue;

                    AssetDatabase.DeleteAsset(assetPath);
                    removed = true;
                }

                if (Directory.GetFiles(folderFullPath, "*.cs").Length == 0)
                {
                    AssetDatabase.DeleteAsset(folder);
                    removed = true;
                }
            }

            return removed;
        }

        private static void EnsureModuleAsmRef(string folder)
        {
            string assetPath = folder + "/FlowIoC.Generated.asmref";
            string fullPath = GetFullPath(assetPath);
            if (File.Exists(fullPath)) return;

            File.WriteAllText(fullPath, ASM_REF_CONTENT);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// The part: the channel, its colour, and its profile when the card declares one. The
        /// colour is a Color32 written in bytes, so the file says exactly what the card said and a
        /// regeneration from an unchanged card writes an identical file.
        /// </summary>
        internal static string GeneratePartContent(ChannelPartEVO channel)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string identifier = SanitizeIdentifier(channel.Name, used);
            string name = EscapeXml(channel.Name);

            var sb = new StringBuilder();

            AppendHeader(sb, true);
            sb.AppendLine("    public static partial class FlowLogType");
            sb.AppendLine("    {");
            sb.AppendLine($"        /// <summary>The {name} channel.</summary>");
            sb.AppendLine($"        public const string {identifier} = \"{channel.Name}\";");
            sb.AppendLine();
            sb.AppendLine(
                $"        /// <summary>The colour {name}'s rows are drawn in. A \"Colour: #RRGGBB\" line above the block in the module's MODULE.md sets it.</summary>");
            sb.AppendLine(
                $"        public static readonly Color {identifier}Color = new Color32({channel.Color.r}, {channel.Color.g}, {channel.Color.b}, {channel.Color.a});");

            if (channel.Profile != null)
            {
                sb.AppendLine();
                sb.AppendLine(
                    $"        /// <summary>What decorates a {name} line: the \"Profile:\" line above the block in the module's MODULE.md.</summary>");
                sb.AppendLine($"        public static readonly FlowLogProfile {identifier}Profile = new FlowLogProfile()");
                AppendProfileCalls(sb, channel.Profile);
            }

            sb.AppendLine("    }");
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// The fluent calls that rebuild the profile at load. Colours go as hex strings, the way
        /// the card wrote them, through the overloads FlowLogProfile already has for a hand-written
        /// profile.
        /// </summary>
        private static void AppendProfileCalls(StringBuilder sb, FlowLogProfile profile)
        {
            var calls = new List<string>();

            if (!string.IsNullOrEmpty(profile.Prefix))
                calls.Add(
                    $".SetPrefix({Literal(profile.Prefix)}, {StyleLiteral(profile.PrefixStyle)}, \"#{FlowLogProfileLine.HexOf(profile.PrefixColor)}\")");

            if (profile.MessageStyle != FlowTextStyle.None)
                calls.Add($".SetMessageStyle({StyleLiteral(profile.MessageStyle)})");

            if (profile.MessageColor != Color.white)
                calls.Add($".SetMessageColor(\"#{FlowLogProfileLine.HexOf(profile.MessageColor)}\")");

            if (!string.IsNullOrEmpty(profile.Postfix))
                calls.Add(
                    $".SetPostfix({Literal(profile.Postfix)}, {StyleLiteral(profile.PostfixStyle)}, \"#{FlowLogProfileLine.HexOf(profile.PostfixColor)}\")");

            for (int index = 0; index < calls.Count; index++)
                sb.AppendLine("            " + calls[index] + (index == calls.Count - 1 ? ";" : ""));
        }

        private static string StyleLiteral(FlowTextStyle style)
        {
            if (style == FlowTextStyle.None) return "FlowTextStyle.None";

            var names = new List<string>();
            if ((style & FlowTextStyle.Bold) != 0) names.Add("FlowTextStyle.Bold");
            if ((style & FlowTextStyle.Italic) != 0) names.Add("FlowTextStyle.Italic");
            if ((style & FlowTextStyle.Underline) != 0) names.Add("FlowTextStyle.Underline");

            return names.Count == 0 ? "FlowTextStyle.None" : string.Join(" | ", names);
        }

        private static string Literal(string text)
        {
            return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// What is left in the shared file once every module's channel is declared in the module:
        /// the project's Default channel, which belongs to no module and so has nowhere else to go.
        /// The file is kept - rather than the package declaring Default itself - because a project
        /// upgrading already has it, and two declarations of one constant would stop the project
        /// compiling before anything could delete the older one.
        /// </summary>
        private static string GenerateClassContent()
        {
            var sb = new StringBuilder();

            AppendHeader(sb, false);
            sb.AppendLine("    public static partial class FlowLogType");
            sb.AppendLine("    {");
            sb.AppendLine($"        /// <summary>The {DEFAULT_CHANNEL} channel.</summary>");
            sb.AppendLine($"        public const string {DEFAULT_CHANNEL} = \"{DEFAULT_CHANNEL}\";");
            sb.AppendLine("    }");
            sb.Append("}");

            return sb.ToString();
        }

        private static void AppendHeader(StringBuilder sb, bool usesUnityEngine)
        {
            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     This code was generated by FlowConsole.");
            sb.AppendLine("//     Do not modify. Changes will be overwritten.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("//------------------------------------------------------------------------------");
            sb.AppendLine();

            if (usesUnityEngine)
            {
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine();
            }

            sb.AppendLine("namespace FlowIoC.ConsoleModule");
            sb.AppendLine("{");
        }

        internal static string SanitizeIdentifier(string name, HashSet<string> usedIdentifiers)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown";

            var sanitized = name.Replace(' ', '_')
                .Replace('-', '_')
                .Replace('.', '_');

            sanitized = Regex.Replace(sanitized, @"[^\w]", "");

            if (sanitized.Length == 0)
                sanitized = "Unknown";
            else if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            string original = sanitized;
            int suffix = 1;
            while (!usedIdentifiers.Add(sanitized))
            {
                sanitized = $"{original}_{suffix}";
                suffix++;
            }

            return sanitized;
        }

        private static string EscapeXml(string text)
        {
            return text.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static void EnsureDirectoryExists()
        {
            string fullPath = GetFullPath(GeneratedFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.ImportAsset(GeneratedFolder, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void EnsureAsmRefExists()
        {
            string fullPath = GetFullPath(AsmRefPath);
            if (File.Exists(fullPath)) return;

            File.WriteAllText(fullPath, ASM_REF_CONTENT);
            AssetDatabase.ImportAsset(AsmRefPath, ImportAssetOptions.ForceUpdate);
        }

        private static string GetFullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath);
        }
    }

    /// <summary>
    /// What a module's part is written from: the card's Colour and Profile lines when it has them,
    /// the palette's pick for the name when it does not. The card is read rather than the index
    /// because the card is the module's own file - the one a person edits - and the index is a
    /// cache the next scan rebuilds.
    /// </summary>
    internal class ChannelPartReader
    {
        private readonly ModuleCardFile _cards = new ModuleCardFile();
        private readonly ModuleCardChannelLines _lines = new ModuleCardChannelLines();
        private readonly FlowLogProfileLine _profileLine = new FlowLogProfileLine();
        private readonly FlowChannelPalette _palette = new FlowChannelPalette();

        internal ChannelPartEVO Read(string channelName, string moduleAbsolutePath)
        {
            return From(channelName, _lines.Read(_cards.Read(moduleAbsolutePath)));
        }

        internal ChannelPartEVO From(string channelName, ModuleCardChannelLinesEVO lines)
        {
            Color32 color = _palette.Pick(channelName);

            if (!string.IsNullOrEmpty(lines.Colour))
            {
                string hex = lines.Colour.StartsWith("#", StringComparison.Ordinal) ? lines.Colour : "#" + lines.Colour;

                if (ColorUtility.TryParseHtmlString(hex, out Color chosen))
                    color = chosen;
            }

            return new ChannelPartEVO
            {
                Name = channelName,
                Color = color,
                Profile = _profileLine.Parse(lines.Profile)
            };
        }
    }
}
#endif