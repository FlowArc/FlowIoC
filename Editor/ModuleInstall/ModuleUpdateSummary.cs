#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The words an update shows: the question before anything is written, and the report after.
    /// Pure text, so what the reader is told is tested rather than eyeballed.
    ///
    /// The question names everything the update does to the game's own work without asking - the
    /// code edits the package's version replaces, the sub modules that go, the references left
    /// on them - and counts the data files it does ask about, which the window lists one by one.
    /// </summary>
    internal class ModuleUpdateSummary
    {
        /// <summary>
        /// The question. references are the project's asmdefs that reference an assembly the
        /// update removes, one line each, as <see cref="RemovedAssemblyReferences"/> words them.
        /// </summary>
        internal string Ask(string title, string from, string to, ModuleUpdatePlanEVO plan,
            IReadOnlyList<string> references = null)
        {
            var text = new StringBuilder();

            text.Append("Update ").Append(title).Append(' ').Append(Shown(from)).Append(" → ").Append(to).Append("\n\n");

            int changed = plan.Count(ModuleUpdateVerdict.Overwrite);
            int added = plan.Count(ModuleUpdateVerdict.Copy);
            int removed = plan.Count(ModuleUpdateVerdict.Delete);

            text.Append(Files(changed)).Append(changed == 1 ? " changes, " : " change, ")
                .Append(Files(added)).Append(added == 1 ? " is added, " : " are added, ")
                .Append(Files(removed)).Append(removed == 1 ? " is removed.\n" : " are removed.\n");

            int edited = plan.Count(ModuleUpdateVerdict.KeepEdited);

            if (edited > 0)
                text.Append(Files(edited)).Append(" you edited ").Append(edited == 1 ? "stays as it is" : "stay as they are").Append(".\n");

            IReadOnlyList<string> replaced = plan.Of(ModuleUpdateVerdict.Replace);

            if (replaced.Count > 0)
            {
                text.Append("\nCode you edited that the update also changes takes the package's version:\n");
                Listed(text, replaced);
            }

            IReadOnlyList<string> removedEdited = plan.Of(ModuleUpdateVerdict.RemoveEdited);

            if (removedEdited.Count > 0)
            {
                text.Append("\nCode you edited that the package no longer ships is removed:\n");
                Listed(text, removedEdited);
            }

            if (plan.RemovedModules.Count > 0)
            {
                text.Append('\n').Append(plan.RemovedModules.Count == 1 ? "A sub module goes" : "Sub modules go")
                    .Append(" whole - the package no longer ships ")
                    .Append(plan.RemovedModules.Count == 1 ? "it" : "them").Append(" there:\n");
                Listed(text, plan.RemovedModules);
            }

            if (references != null && references.Count > 0)
            {
                text.Append("\nYour project references ").Append(references.Count == 1 ? "an assembly" : "assemblies")
                    .Append(" the update removes. Take the reference and the code using it out, or it will not compile:\n");
                Listed(text, references);
            }

            int conflicts = plan.Count(ModuleUpdateVerdict.Conflict);

            if (conflicts > 0)
            {
                text.Append('\n').Append(Files(conflicts))
                    .Append(conflicts == 1 ? " of yours meets" : " of yours meet")
                    .Append(" a change of the package's - choose for each below.\n");
            }

            if (!plan.HadRecord)
            {
                text.Append("\nThis module was installed before it kept a record, so every file that "
                            + "differs from the shipped one is read as changed on both sides.\n");
            }

            text.Append("\nCommit or back up before updating.");

            return text.ToString();
        }

        /// <summary>The report. takeTheirs are the conflicts the reader gave to the package.</summary>
        internal string Done(string title, string to, ModuleUpdatePlanEVO plan, ICollection<string> takeTheirs)
        {
            int written = plan.Count(ModuleUpdateVerdict.Overwrite) + plan.Count(ModuleUpdateVerdict.Copy)
                                                                    + plan.Count(ModuleUpdateVerdict.Replace);
            int removed = plan.Count(ModuleUpdateVerdict.Delete) + plan.Count(ModuleUpdateVerdict.RemoveEdited);

            var kept = new List<string>(plan.Of(ModuleUpdateVerdict.KeepEdited));

            foreach (string conflict in plan.Of(ModuleUpdateVerdict.Conflict))
            {
                if (takeTheirs == null || !takeTheirs.Contains(conflict))
                    kept.Add(conflict);
                else if (plan.Unshipped.Contains(conflict))
                    removed++;
                else
                    written++;
            }

            kept.Sort(StringComparer.Ordinal);

            var text = new StringBuilder();

            text.Append(title).Append(" is now ").Append(to).Append(".\n\n")
                .Append(Files(written)).Append(" written, ").Append(removed).Append(" removed.");

            if (kept.Count > 0)
                text.Append("\nKept yours: ").Append(string.Join(", ", kept)).Append('.');

            return text.ToString();
        }

        private static string Shown(string version) =>
            string.IsNullOrEmpty(version) || version == ModuleCardVersionLine.NONE
                ? "(no version recorded)"
                : version;

        private static string Files(int count) => count == 1 ? "1 file" : count + " files";

        private static void Listed(StringBuilder text, IReadOnlyList<string> paths)
        {
            foreach (string path in paths)
                text.Append("  ").Append(path).Append('\n');
        }
    }
}

#endif