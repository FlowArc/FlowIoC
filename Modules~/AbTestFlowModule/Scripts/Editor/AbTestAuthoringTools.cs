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
    /// dirtying and saving are Unity's: add a test with sane defaults, add a group or an override
    /// to it, raise its version, remove any of them. Kept apart from the drawing so the tests
    /// reach it without a window - the panel only says where the buttons are.
    ///
    /// A new element of a serialized list copies the element before it, which for a test would
    /// mean a second test with the same id and for a group the same name. Every Add here clears
    /// what it appended.
    /// </summary>
    internal class AbTestAuthoringTools
    {
        internal const string TESTS = "_tests";
        internal const string ID = "Id";
        internal const string VERSION = "Version";
        internal const string IS_ACTIVE = "IsActive";
        internal const string ROLLOUT_PERCENT = "RolloutPercent";
        internal const string GROUPS = "Groups";
        internal const string NAME = "Name";
        internal const string OVERRIDES = "Overrides";
        internal const string ORIGINAL = "Original";
        internal const string VARIANT = "Variant";

        private const string CONTROL_GROUP = "control";
        private const string VARIANT_GROUP = "variant";

        private readonly AbTestConfigValidator _validator = new AbTestConfigValidator();

        internal SerializedProperty Tests(SerializedObject asset) => asset.FindProperty(TESTS);

        /// <summary>
        /// A test the validator has nothing to say about: active, rolled out to everybody, with
        /// the control group and one variant. The id is the one thing only the author can give.
        /// </summary>
        internal SerializedProperty AddTest(SerializedObject asset)
        {
            SerializedProperty tests = Tests(asset);
            tests.arraySize++;

            SerializedProperty test = tests.GetArrayElementAtIndex(tests.arraySize - 1);
            test.FindPropertyRelative(ID).stringValue = string.Empty;
            test.FindPropertyRelative(VERSION).intValue = 1;
            test.FindPropertyRelative(IS_ACTIVE).boolValue = true;
            test.FindPropertyRelative(ROLLOUT_PERCENT).floatValue = 100f;

            SerializedProperty groups = test.FindPropertyRelative(GROUPS);
            groups.ClearArray();
            AddGroup(test).FindPropertyRelative(NAME).stringValue = CONTROL_GROUP;
            AddGroup(test).FindPropertyRelative(NAME).stringValue = VARIANT_GROUP;

            return test;
        }

        internal SerializedProperty AddGroup(SerializedProperty test)
        {
            SerializedProperty groups = test.FindPropertyRelative(GROUPS);
            groups.arraySize++;

            SerializedProperty group = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            group.FindPropertyRelative(NAME).stringValue = string.Empty;
            group.FindPropertyRelative(OVERRIDES).ClearArray();

            return group;
        }

        internal SerializedProperty AddOverride(SerializedProperty group)
        {
            SerializedProperty overrides = group.FindPropertyRelative(OVERRIDES);
            overrides.arraySize++;

            SerializedProperty pair = overrides.GetArrayElementAtIndex(overrides.arraySize - 1);
            pair.FindPropertyRelative(ORIGINAL).objectReferenceValue = null;
            pair.FindPropertyRelative(VARIANT).objectReferenceValue = null;

            return pair;
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

            foreach (AbTestValidationVO message in _validator.Validate(asset.Tests))
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
    }
}

#endif
