#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Applies a plan to an installed module: deletes, copies, overwrites, resolves every
    /// conflict the one way it was told, then rewrites the record and the card's version line.
    ///
    /// Nothing is read here that the plan did not already read, so the dialog the plan was
    /// shown in described exactly this pass. A file that cannot be written stops the pass and is
    /// named; what was already written stays written, which is what the dialog's "commit first"
    /// is for.
    /// </summary>
    internal class ModuleUpdater
    {
        private const string META = ".meta";

        private readonly ShippedRecord _record = new ShippedRecord();
        private readonly ModuleCardVersionLine _version = new ModuleCardVersionLine();
        private readonly ModuleCardFile _card = new ModuleCardFile();

        internal bool TryApply(ModuleUpdatePlanEVO plan, string installedFolder, string shippedFolder,
            ConflictPolicy policy, out string error)
        {
            error = null;

            foreach (KeyValuePair<string, ModuleUpdateVerdict> pair in plan.Verdicts)
            {
                try
                {
                    Apply(pair.Key, pair.Value, installedFolder, shippedFolder, policy);
                }
                catch (Exception exception)
                {
                    error = $"FlowIoC could not update '{pair.Key}': {exception.Message}";
                    return false;
                }
            }

            try
            {
                _record.Write(installedFolder, _record.Read(shippedFolder) ?? _record.Build(shippedFolder));

                // The version goes onto whichever card the pass left behind. A shipped copy with
                // no version has nothing to write, and 0.0.0 on the card would say otherwise.
                string card = _card.Read(installedFolder);
                string version = _version.Read(_card.Read(shippedFolder));

                if (card != null && version != ModuleCardVersionLine.NONE)
                    _card.Write(installedFolder, _version.Write(card, version));
            }
            catch (Exception exception)
            {
                error = $"FlowIoC could not write the module's record: {exception.Message}";
                return false;
            }

            return true;
        }

        private static void Apply(string path, ModuleUpdateVerdict verdict, string installed, string shipped,
            ConflictPolicy policy)
        {
            switch (verdict)
            {
                case ModuleUpdateVerdict.Overwrite:
                case ModuleUpdateVerdict.Copy:
                case ModuleUpdateVerdict.Regenerate:
                    CopyOver(shipped, installed, path);
                    return;

                case ModuleUpdateVerdict.Delete:
                    DeleteWithMeta(installed, path);
                    return;

                case ModuleUpdateVerdict.Conflict:
                    if (policy == ConflictPolicy.KeepMine)
                        return;

                    // The package's side of a conflict is the file it ships now, or no file at all.
                    if (File.Exists(Full(shipped, path)))
                        CopyOver(shipped, installed, path);
                    else
                        DeleteWithMeta(installed, path);

                    return;

                default:
                    return;
            }
        }

        private static void CopyOver(string shipped, string installed, string path)
        {
            string target = Full(installed, path);

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(Full(shipped, path), target, true);
        }

        /// <summary>
        /// A meta is a tracked file with a verdict of its own, so it normally goes on its own
        /// line of the plan. It is deleted here as well for the one case it does not: a meta the
        /// game touched beside a file the package removed, which would otherwise stay behind and
        /// have Unity report it.
        /// </summary>
        private static void DeleteWithMeta(string installed, string path)
        {
            string target = Full(installed, path);

            if (File.Exists(target))
                File.Delete(target);

            if (!path.EndsWith(META, StringComparison.OrdinalIgnoreCase) && File.Exists(target + META))
                File.Delete(target + META);
        }

        /// <summary>Through LongPath: the shipped side is read out of the package cache, often past 260 characters.</summary>
        private static string Full(string root, string relative) =>
            new LongPath().Of(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
    }
}

#endif