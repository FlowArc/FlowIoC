#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// A log names no channel. The module is read off the file the call sits in, so inside a
    /// module the channel argument is never right: a string typed by hand lands on a channel no
    /// module owns - no colour, no switch, no card - and the module's own constant says nothing
    /// the path did not, then keeps saying it after the line is pasted into another module.
    ///
    /// Reported, per module, for every call whose first argument is a string literal, or is
    /// <c>FlowModule.Default</c>, or is the constant of the very module the file sits in. A
    /// Connector naming the module it wires, and any line naming some other module's constant, is
    /// left alone: that is the one place the channel is meant to be written.
    ///
    /// The repair is mechanical, which is why the finding is Fixable. Where the message is all
    /// that follows, the channel is cut and the call becomes the channel-less overload. Where more
    /// follows - a profile, a context object - a literal is replaced by the module's constant,
    /// because no channel-less overload takes a profile; the module's own constant in that shape
    /// is not reported at all, since the docs allow a profile to name its channel. A literal that
    /// is exactly another module's name becomes that module's constant: the line was about that
    /// module, and only the spelling was wrong.
    ///
    /// One shape is read and left alone: <c>LogError("Nope", something)</c>, a bare word and then
    /// a value that is no string. It is either a typed channel with a variable message or the
    /// channel-less overload with its context object, and the text cannot say which - so it is
    /// neither reported nor repaired, rather than repaired wrong.
    ///
    /// A test module's files log on the module they test, so its own channel is its parent's.
    /// The files of a nested module are that module's to report, not its parent's.
    /// </summary>
    internal class LogChannelCheck : IModuleCheck
    {
        private const string FLOW_MODULE_PREFIX = "FlowModule.";
        private const int NAMED_IN_MESSAGE = 6;

        private static readonly Regex Constant = new Regex("^FlowModule\\.(?<name>[A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
        private static readonly Regex PlainLiteral = new Regex("^\"(?<name>[A-Za-z_][A-Za-z0-9_]*)\"$", RegexOptions.Compiled);

        private readonly Func<ModuleTargetEVO, IEnumerable<string>> _sourcesOf;
        private readonly Func<string, string> _read;
        private readonly Action<string, Func<string, string>> _rewrite;
        private readonly Func<string, bool> _isModuleName;
        private readonly LogCallReader _reader = new LogCallReader();

        internal LogChannelCheck() : this(
            DefaultSourcesOf,
            File.ReadAllText,
            (path, rewrite) => new TextFile().Rewrite(path, rewrite),
            DefaultIsModuleName)
        {
        }

        internal LogChannelCheck(
            Func<ModuleTargetEVO, IEnumerable<string>> sourcesOf,
            Func<string, string> read,
            Action<string, Func<string, string>> rewrite,
            Func<string, bool> isModuleName)
        {
            _sourcesOf = sourcesOf;
            _read = read;
            _rewrite = rewrite;
            _isModuleName = isModuleName;
        }

        public string Id => "log-channels";

        public FindingEVO Inspect(ModuleTargetEVO module)
        {
            string own = OwnChannelOf(module);
            var named = new List<string>();
            string firstFile = null;

            foreach (string file in _sourcesOf(module))
            {
                List<Repair> repairs = RepairsIn(_read(file), own);
                if (repairs.Count == 0) continue;

                firstFile ??= file;

                foreach (Repair repair in repairs)
                    named.Add(Path.GetFileName(file) + ":" + repair.Call.Line);
            }

            if (named.Count == 0)
                return FindingEVO.Ok(Id, "Log channels");

            string where = string.Join(", ", named.Take(NAMED_IN_MESSAGE));
            if (named.Count > NAMED_IN_MESSAGE)
                where += $" and {named.Count - NAMED_IN_MESSAGE} more";

            string lines = named.Count == 1 ? "line names" : "lines name";

            return FindingEVO.Fixable(
                Id,
                $"{named.Count} {lines} a channel by hand: {where}. A log names no channel - the module is "
                + "read off the file, and a line copied into another module then lands on that module by "
                + "itself. Fix cuts the channel, or writes the module's constant where a profile or a "
                + "context follows the message.",
                AssetPathOf(module, firstFile));
        }

        public void Fix(ModuleTargetEVO module)
        {
            string own = OwnChannelOf(module);

            foreach (string file in _sourcesOf(module))
                _rewrite(file, text => Rewrite(text, own));
        }

        /// <summary>
        /// The text with every reported call repaired, applied last to first so that an earlier
        /// span is still where the reader found it when its turn comes.
        /// </summary>
        internal string Rewrite(string text, string own)
        {
            List<Repair> repairs = RepairsIn(text, own);

            for (int index = repairs.Count - 1; index >= 0; index--)
            {
                Repair repair = repairs[index];
                ArgumentSpanEVO channel = repair.Call.Arguments[0];

                if (repair.Replacement == null)
                {
                    // The channel and the comma after it go; the message keeps its own position.
                    int messageStart = repair.Call.Arguments[1].Start;
                    text = text.Remove(channel.Start, messageStart - channel.Start);
                }
                else
                {
                    text = text.Substring(0, channel.Start) + repair.Replacement + text.Substring(channel.End);
                }
            }

            return text;
        }

        private List<Repair> RepairsIn(string text, string own)
        {
            var repairs = new List<Repair>();

            foreach (LogCallEVO call in _reader.Read(text))
            {
                if (TryClassify(call, own, out string replacement))
                    repairs.Add(new Repair(call, replacement));
            }

            return repairs;
        }

        /// <summary>
        /// Whether the call names a channel it should not, and what the first argument becomes:
        /// null to cut it, otherwise the constant that replaces it.
        /// </summary>
        private bool TryClassify(LogCallEVO call, string own, out string replacement)
        {
            replacement = null;

            // One argument is already the channel-less overload; none is not a call worth reading.
            if (call.Arguments.Count < 2) return false;

            string first = call.Arguments[0].Text;
            bool messageOnly = call.Arguments.Count == 2;

            if (IsStringLiteral(first))
            {
                Match plain = PlainLiteral.Match(first);
                string literal = plain.Success ? plain.Groups["name"].Value : null;

                // LogError("Save failed.", _config) is the channel-less overload with its context:
                // a literal message, then something that is not a string. Only a bare word in
                // the first place could still be a channel, and with a variable message after it
                // the text cannot tell the two apart - that one is left alone rather than guessed.
                if (messageOnly && call.Method == "LogError" && !IsStringLiteral(call.Arguments[1].Text))
                    return false;

                if (literal != null && literal != own && literal != FlowModule.Default && _isModuleName(literal))
                {
                    replacement = FLOW_MODULE_PREFIX + literal;
                    return true;
                }

                if (!messageOnly)
                    replacement = FLOW_MODULE_PREFIX + own;

                return true;
            }

            Match constant = Constant.Match(first);
            if (!constant.Success) return false;

            string name = constant.Groups["name"].Value;

            if (name == own)
                return messageOnly;

            if (name != FlowModule.Default) return false;

            if (!messageOnly)
                replacement = FLOW_MODULE_PREFIX + own;

            return true;
        }

        private static bool IsStringLiteral(string argument)
        {
            int index = 0;
            while (index < argument.Length && (argument[index] == '@' || argument[index] == '$')) index++;

            return index <= 2 && index < argument.Length && argument[index] == '"';
        }

        /// <summary>
        /// The channel this module's files log on when they name none: the module itself, or for a
        /// test module the module it tests, which is where the resolver sends its lines.
        /// </summary>
        internal static string OwnChannelOf(ModuleTargetEVO module)
        {
            if (module.Kind == ModuleKind.Test && !string.IsNullOrEmpty(module.ParentName))
                return module.ParentName;

            return module.Name;
        }

        /// <summary>
        /// Every .cs file that is this module's own. A file under a nested module - any folder
        /// named *Module below this one - belongs to that module and is inspected with it.
        /// </summary>
        private static IEnumerable<string> DefaultSourcesOf(ModuleTargetEVO module)
        {
            if (module == null || string.IsNullOrEmpty(module.AbsolutePath) || !Directory.Exists(module.AbsolutePath))
                return Enumerable.Empty<string>();

            return Directory.GetFiles(module.AbsolutePath, "*.cs", SearchOption.AllDirectories)
                .Where(file => IsOwnFile(module.AbsolutePath, file))
                .OrderBy(file => file, StringComparer.Ordinal);
        }

        internal static bool IsOwnFile(string moduleRoot, string file)
        {
            string relative = file.Substring(moduleRoot.Length).Replace('\\', '/').TrimStart('/');
            string[] segments = relative.Split('/');

            for (int index = 0; index < segments.Length - 1; index++)
            {
                if (segments[index].EndsWith("Module", StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static bool DefaultIsModuleName(string name)
        {
            FieldInfo field = typeof(FlowModule).GetField(name, BindingFlags.Public | BindingFlags.Static);
            return field != null && field.IsLiteral && field.FieldType == typeof(string);
        }

        private static string AssetPathOf(ModuleTargetEVO module, string file)
        {
            if (string.IsNullOrEmpty(module.AssetPath) || string.IsNullOrEmpty(module.AbsolutePath)) return null;

            string relative = file.Replace('\\', '/');
            string root = module.AbsolutePath.Replace('\\', '/').TrimEnd('/');

            if (!relative.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return module.AssetPath;

            return module.AssetPath.Replace('\\', '/').TrimEnd('/') + relative.Substring(root.Length);
        }

        private readonly struct Repair
        {
            internal LogCallEVO Call { get; }
            internal string Replacement { get; }

            internal Repair(LogCallEVO call, string replacement)
            {
                Call = call;
                Replacement = replacement;
            }
        }
    }
}

#endif