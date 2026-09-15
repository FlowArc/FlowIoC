#if UNITY_EDITOR

using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Shared.Constants;
using UnityEngine;

namespace Modules.AbTestFlowModule.Editor
{
    /// <summary>
    /// What the panel does to a player's assignments, without a window in the way so a test can
    /// do it too: read the group PlayerPrefs holds for a test, force one, or forget it. It writes
    /// the key in the module's own shape - "&lt;version&gt;|&lt;group&gt;" under the prefix in
    /// AbTestConstants - so the next run reads a forced group exactly as it reads a rolled one.
    /// Nothing here touches the Model: the module rolls and files at boot, from these prefs.
    /// </summary>
    internal class AbTestPrefsTools
    {
        /// <summary>What a stored assignment says, or why it will not stand at the next run.</summary>
        internal enum Standing
        {
            NotAssigned = 0,
            InGroup = 1,
            OutOfTest = 2,
            Stale = 3,
            UnknownGroup = 4
        }

        internal struct StoredAssignment
        {
            internal Standing Standing;
            internal int Version;
            internal string Group;
        }

        internal StoredAssignment Read(AbTestCVO test)
        {
            string stored = PlayerPrefs.GetString(Key(test.Id), string.Empty);

            if (string.IsNullOrEmpty(stored))
                return new StoredAssignment {Standing = Standing.NotAssigned};

            string[] parts = stored.Split('|');

            if (parts.Length != 2 || !int.TryParse(parts[0], out int version))
                return new StoredAssignment {Standing = Standing.NotAssigned};

            var assignment = new StoredAssignment {Version = version, Group = parts[1]};

            if (version != test.Version)
                assignment.Standing = Standing.Stale;
            else if (parts[1] == AbTestConstants.OutOfTestMarker)
                assignment.Standing = Standing.OutOfTest;
            else if (HasGroup(test, parts[1]))
                assignment.Standing = Standing.InGroup;
            else
                assignment.Standing = Standing.UnknownGroup;

            return assignment;
        }

        /// <summary>Puts the player in a group of the test, under the test's current version.</summary>
        internal void Force(AbTestCVO test, string group)
        {
            PlayerPrefs.SetString(Key(test.Id), $"{test.Version}|{group}");
            PlayerPrefs.Save();
        }

        /// <summary>Puts the player outside the test, the way a rollout the player missed would.</summary>
        internal void ForceOutside(AbTestCVO test) => Force(test, AbTestConstants.OutOfTestMarker);

        /// <summary>Forgets the assignment, so the next run rolls the test again.</summary>
        internal void Reset(AbTestCVO test)
        {
            PlayerPrefs.DeleteKey(Key(test.Id));
            PlayerPrefs.Save();
        }

        private static string Key(string id) => AbTestConstants.PrefsPrefix + id;

        private static bool HasGroup(AbTestCVO test, string groupName)
        {
            foreach (AbTestGroupCVO group in test.Groups)
            {
                if (group.Name == groupName)
                    return true;
            }

            return false;
        }
    }
}

#endif
