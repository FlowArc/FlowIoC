#if UNITY_EDITOR

using System.Collections.Generic;
using Modules.AbTestFlowModule.Data.UnityObjects;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Services;
using UnityEditor;

namespace Modules.AbTestFlowModule.Editor
{
    /// <summary>
    /// What the editor panel does to a CD_AbTests asset, through its SerializedObject so undo,
    /// dirtying and saving are Unity's: add a test with sane defaults, add a group or an asset row
    /// to it, raise its version, remove any of them. Kept apart from the drawing so the tests
    /// reach it without a window - the panel only says where the buttons are.
    ///
    /// A test's groups are drawn as a matrix - a row per asset the control group lists, a column
    /// per group - so a row is added to and removed from every group at once, and a new group
    /// starts with an empty slot per row, which keeps every column the length of the control's.
    ///
    /// A new element of a serialized list copies the element before it, which for a test would
    /// mean a second test with the same id and for a group the same name and assets. Every Add
    /// here clears what it appended.
    /// </summary>
    internal class AbTestAuthoringTools
    {
        internal const string ACTIVE_TEST_ID = "_activeTestId";
        internal const string TESTS = "_tests";
        internal const string ID = "Id";
        internal const string VERSION = "Version";
        internal const string TEST_USER_PERCENT = "TestUserPercent";
        internal const string GROUPS = "Groups";
        internal const string NAME = "Name";
        internal const string ASSETS = "Assets";

        private const string CONTROL_GROUP = "control";
        private const string VARIANT_GROUP = "variant";

        private readonly AbTestConfigValidator _validator = new AbTestConfigValidator();

        internal SerializedProperty Tests(SerializedObject asset) => asset.FindProperty(TESTS);

        internal SerializedProperty ActiveTestId(SerializedObject asset) => asset.FindProperty(ACTIVE_TEST_ID);

        /// <summary>The one test that runs, by id; an empty id runs none.</summary>
        internal void Activate(SerializedObject asset, string id) => ActiveTestId(asset).stringValue = id ?? string.Empty;

        /// <summary>
        /// A test the validator has nothing to say about: every player in it, with the control group
        /// and one variant and no asset yet. The id is the one thing only the author can give, and
        /// making it the active test is a second, deliberate act - a new test never displaces the
        /// one that is running.
        /// </summary>
        internal SerializedProperty AddTest(SerializedObject asset)
        {
            SerializedProperty tests = Tests(asset);
            tests.arraySize++;

            SerializedProperty test = tests.GetArrayElementAtIndex(tests.arraySize - 1);
            test.FindPropertyRelative(ID).stringValue = string.Empty;
            test.FindPropertyRelative(VERSION).intValue = 1;
            test.FindPropertyRelative(TEST_USER_PERCENT).floatValue = 100f;

            SerializedProperty groups = test.FindPropertyRelative(GROUPS);
            groups.ClearArray();
            AddGroup(test).FindPropertyRelative(NAME).stringValue = CONTROL_GROUP;
            AddGroup(test).FindPropertyRelative(NAME).stringValue = VARIANT_GROUP;

            return test;
        }

        /// <summary>A new group has no name and an empty slot for every asset the control lists.</summary>
        internal SerializedProperty AddGroup(SerializedProperty test)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);
            int rows = Rows(test);
            groups.arraySize++;

            SerializedProperty group = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            group.FindPropertyRelative(NAME).stringValue = string.Empty;

            SerializedProperty assets = group.FindPropertyRelative(ASSETS);
            assets.ClearArray();

            for (var row = 0; row < rows; row++)
                AppendEmptySlot(assets);

            return group;
        }

        /// <summary>One more asset the test changes: an empty slot at the end of every group.</summary>
        internal void AddAsset(SerializedProperty test)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);

            for (var i = 0; i < groups.arraySize; i++)
                AppendEmptySlot(groups.GetArrayElementAtIndex(i).FindPropertyRelative(ASSETS));
        }

        /// <summary>Takes one row out of every group that has it.</summary>
        internal void RemoveAsset(SerializedProperty test, int row)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);

            for (var i = 0; i < groups.arraySize; i++)
            {
                SerializedProperty assets = groups.GetArrayElementAtIndex(i).FindPropertyRelative(ASSETS);

                if (row >= assets.arraySize)
                    continue;

                // A reference slot that is not empty is only emptied by the first delete, on some
                // Unity versions - so it is emptied first and the delete is the delete everywhere.
                assets.GetArrayElementAtIndex(row).objectReferenceValue = null;
                assets.DeleteArrayElementAtIndex(row);
            }
        }

        /// <summary>
        /// Every group's list brought to the length of the longest, with empty slots, so the
        /// matrix has a cell for every row - a list hand-edited to a different length is repaired
        /// with nothing lost, and the validator's empty-slot error says where to look.
        /// </summary>
        internal void AlignAssets(SerializedProperty test)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);
            var rows = 0;

            for (var i = 0; i < groups.arraySize; i++)
            {
                int count = groups.GetArrayElementAtIndex(i).FindPropertyRelative(ASSETS).arraySize;

                if (count > rows)
                    rows = count;
            }

            for (var i = 0; i < groups.arraySize; i++)
            {
                SerializedProperty assets = groups.GetArrayElementAtIndex(i).FindPropertyRelative(ASSETS);

                while (assets.arraySize < rows)
                    AppendEmptySlot(assets);
            }
        }

        /// <summary>How many assets the test changes: the control group's count, or none without one.</summary>
        internal int Rows(SerializedProperty test)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);

            return groups.arraySize > 0
                ? groups.GetArrayElementAtIndex(0).FindPropertyRelative(ASSETS).arraySize
                : 0;
        }

        /// <summary>Restarts the test: every player decides again at their next launch.</summary>
        internal void RaiseVersion(SerializedProperty test) => test.FindPropertyRelative(VERSION).intValue++;

        internal void Remove(SerializedProperty list, int index) => list.DeleteArrayElementAtIndex(index);

        /// <summary>
        /// The validator's messages, each under the test it names - or under an empty id when the
        /// message names none, which is what an unnamed test gets. The validator runs over the
        /// whole list because a duplicate id is only visible there.
        /// </summary>
        internal Dictionary<string, List<AbTestValidationVO>> Messages(CD_AbTests asset)
        {
            var byTest = new Dictionary<string, List<AbTestValidationVO>>();

            foreach (AbTestValidationVO message in _validator.Validate(asset.Tests, asset.ActiveTestId))
            {
                string owner = string.Empty;

                foreach (AbTestCVO test in asset.Tests)
                {
                    if (!string.IsNullOrEmpty(test.Id) && message.Message.Contains("'" + test.Id + "'"))
                    {
                        owner = test.Id;
                        break;
                    }
                }

                if (!byTest.TryGetValue(owner, out List<AbTestValidationVO> messages))
                {
                    messages = new List<AbTestValidationVO>();
                    byTest[owner] = messages;
                }

                messages.Add(message);
            }

            return byTest;
        }

        private static void AppendEmptySlot(SerializedProperty assets)
        {
            assets.arraySize++;
            assets.GetArrayElementAtIndex(assets.arraySize - 1).objectReferenceValue = null;
        }
    }
}

#endif