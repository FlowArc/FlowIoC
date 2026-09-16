#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The words of the two dialogs an update shows: the question before anything is written,
    /// and the report after. Pure text, so what the reader is told is tested rather than eyeballed.
    /// </summary>
    internal class ModuleUpdateSummary
    {
        internal string Ask(string title, string from, string to, ModuleUpdatePlanEVO plan)
        {
            var text = new StringBuilder();

            text.Append("Update ").Append(title).Append(' ').Append(Shown(from)).Append(" → ").Append(to).Append("\n\n");

            text.Append(Files(plan.Count(ModuleUpdateVerdict.Overwrite))).Append(" change, ")
                .Append(Files(plan.Count(ModuleUpdateVerdict.Copy))).Append(" is added, ")
                .Append(Files(plan.Count(ModuleUpdateVerdict.Delete))).Append(" is removed.\n");

            int edited = plan.Count(ModuleUpdateVerdict.KeepEdited);

            if (edited > 0)
                text.Append(Files(edited)).Append(" you edited ").Append(edited == 1 ? "stays as it is" : "stay as they are").Append(".\n");

            IReadOnlyList<string> keptRemoved = plan.Of(ModuleUpdateVerdict.KeepRemoved);

            if (keptRemoved.Count > 0)
            {
                text.Append('\n').Append(Files(keptRemoved.Count)).Append(" the update removes ")
                    .Append(keptRemoved.Count == 1 ? "stays" : "stay").Append(" because you edited ")
                    .Append(keptRemoved.Count == 1 ? "it" : "them").Append(":\n");
                Listed(text, keptRemoved);
            }

            IReadOnlyList<string> conflicts = plan.Of(ModuleUpdateVerdict.Conflict);

            if (conflicts.Count > 0)
            {
                text.Append('\n').Append(conflicts.Count).Append(conflicts.Count == 1 ? " conflict" : " conflicts")
                    .Append(" - you edited ").Append(conflicts.Count == 1 ? "it" : "them")
                    .Append(" and the update changes ").Append(conflicts.Count == 1 ? "it" : "them").Append(":\n");
                Listed(text, conflicts);
            }

            if (!plan.HadRecord)
            {
                text.Append("\nThis module was installed before it kept a record, so every file that "
                            + "differs from the shipped one is listed as a conflict.\n");
            }

            text.Append("\nCommit or back up before updating.");

            return text.ToString();
        }

        internal string Done(string title, string to, ModuleUpdatePlanEVO plan, bool keptMine)
        {
            int written = plan.Count(ModuleUpdateVerdict.Overwrite) + plan.Count(ModuleUpdateVerdict.Copy);
            int removed = plan.Count(ModuleUpdateVerdict.Delete);

            var kept = new List<string>();
            kept.AddRange(plan.Of(ModuleUpdateVerdict.KeepEdited));
            kept.AddRange(plan.Of(ModuleUpdateVerdict.KeepRemoved));

            if (keptMine)
                kept.AddRange(plan.Of(ModuleUpdateVerdict.Conflict));
            else
                written += plan.Count(ModuleUpdateVerdict.Conflict);

            kept.Sort(System.StringComparer.Ordinal);

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
