#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.Modules;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule
{
    /// <summary>One nested module that carries the name of the module being renamed, and what becomes of it.</summary>
    internal class FollowerRenameEVO
    {
        internal ModuleTreeRowEVO<ModulePickEVO> Row { get; set; }
        internal string OldName { get; set; }

        /// <summary>The name after the rename - the old one when it does not follow.</summary>
        internal string NewName { get; set; }

        internal bool Follows { get; set; }

        /// <summary>Why it keeps its name, when it does.</summary>
        internal string Reason { get; set; }
    }

    /// <summary>
    /// The modules inside the one being renamed that carry its name and so change with it.
    ///
    /// Create Module names a test module after the module it exercises and a screen module's test
    /// module after the screen, so CounterTestModule under CounterModule and
    /// GameplayScreenTestModule under GameplayScreenModule are written from the parent's name. The
    /// rule is the prefix: a nested module whose name starts with its parent's full stem gets that
    /// stem replaced. It runs through the chain, because a screen's test module carries the screen's
    /// new name, not the top module's.
    ///
    /// The prefix is a rule of thumb rather than a proof - CounterfeitModule under CounterModule
    /// starts with Counter too - which is why every carrier is listed with a tick the reader can
    /// take off. A carrier under one that keeps its name keeps its own: its new name would be built
    /// on a parent name that is not changing.
    /// </summary>
    internal class RenameFollowers
    {
        private readonly ModuleStems _stems = new ModuleStems();

        /// <summary>
        /// Every carrier under <paramref name="picked"/>, in tree order - a parent before what it
        /// holds - with <see cref="FollowerRenameEVO.Follows"/> saying whether it changes.
        /// <paramref name="ticked"/> answers by module name whether the reader left its tick on.
        /// </summary>
        internal List<FollowerRenameEVO> Of(
            ModuleTreeRowEVO<ModulePickEVO> picked, string newPickedName, Func<string, bool> ticked)
        {
            var newNames = new Dictionary<ModuleTreeRowEVO<ModulePickEVO>, string> {[picked] = newPickedName};
            var followers = new List<FollowerRenameEVO>();

            foreach (ModuleTreeRowEVO<ModulePickEVO> row in picked.Descendants)
            {
                ModuleTreeRowEVO<ModulePickEVO> parent = row.Parent;

                if (parent == null || !newNames.TryGetValue(parent, out string parentNewName)) continue;

                string parentOldStem = _stems.FullStem(parent.Row.Name);
                string parentNewStem = _stems.FullStem(parentNewName);

                if (!_stems.Carries(row.Row.Name, parentOldStem)) continue;

                var follower = new FollowerRenameEVO {Row = row, OldName = row.Row.Name, NewName = row.Row.Name};

                if (parentNewName == parent.Row.Name)
                {
                    follower.Reason = "keeps its name: so does " + parent.Row.Name + ".";
                }
                else if (!ticked(row.Row.Name))
                {
                    follower.Reason = "keeps its name: unticked.";
                }
                else
                {
                    follower.NewName = _stems.Carried(row.Row.Name, parentOldStem, parentNewStem);
                    follower.Follows = true;
                }

                // Recorded whether or not it follows, so a child under an unticked carrier finds
                // its parent and learns that the parent keeps its name.
                newNames[row] = follower.NewName;
                followers.Add(follower);
            }

            return followers;
        }
    }
}
#endif
