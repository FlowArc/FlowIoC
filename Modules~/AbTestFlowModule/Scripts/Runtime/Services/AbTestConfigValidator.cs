using System;
using System.Collections.Generic;
using System.Reflection;
using Modules.AbTestFlowModule.Data.ValueObjects;
using Modules.AbTestFlowModule.Enums;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.AbTestFlowModule.Services
{
    /// <summary>
    /// Reads a config the way a reviewer would and says what is wrong with it. Kept apart from the
    /// asset so the rules can be tested without an Editor; the AB Test Editor shows its word under
    /// the test it is about, so a designer hears about a mistake while making it rather than at boot.
    /// </summary>
    public class AbTestConfigValidator
    {
        public List<AbTestValidationVO> Validate(List<AbTestCVO> tests, string activeTestId)
        {
            var messages = new List<AbTestValidationVO>();
            var seen = new HashSet<string>();

            foreach (AbTestCVO test in tests)
                ValidateOne(test, seen, messages);

            ValidateActive(activeTestId, seen, messages);

            return messages;
        }

        /// <summary>
        /// One test runs at a time, and the asset names it. A name no test carries is the one
        /// mistake that runs nothing without a word at boot, so it is an error here.
        /// </summary>
        private void ValidateActive(string activeTestId, HashSet<string> ids, List<AbTestValidationVO> messages)
        {
            if (string.IsNullOrEmpty(activeTestId))
                Add(messages, AbTestValidationSeverity.Information, "No test is active.");
            else if (!ids.Contains(activeTestId))
                Add(messages, AbTestValidationSeverity.Error, $"The active test '{activeTestId}' is not defined.");
        }

        private void ValidateOne(AbTestCVO test, HashSet<string> seen, List<AbTestValidationVO> messages)
        {
            string label = string.IsNullOrEmpty(test.Id) ? "an experiment" : $"'{test.Id}'";

            if (string.IsNullOrEmpty(test.Id))
                Add(messages, AbTestValidationSeverity.Error, "An experiment has no id.");
            else if (!seen.Add(test.Id))
                Add(messages, AbTestValidationSeverity.Error, $"The id '{test.Id}' is used more than once.");

            if (test.Groups.Count < 2)
                Add(messages, AbTestValidationSeverity.Error, $"{label} needs at least two groups.");

            if (test.TestUserPercent <= 0f)
                Add(messages, AbTestValidationSeverity.Information, $"{label} puts nobody in the test.");

            ValidateGroupNames(test, label, messages);
            ValidateAssets(test, label, messages);
        }

        private void ValidateGroupNames(AbTestCVO test, string label, List<AbTestValidationVO> messages)
        {
            var names = new HashSet<string>();

            for (var i = 0; i < test.Groups.Count; i++)
            {
                AbTestGroupCVO group = test.Groups[i];

                if (string.IsNullOrEmpty(group.Name))
                    Add(messages, AbTestValidationSeverity.Error, $"A group of {label} has no name.", i);
                else if (!names.Add(group.Name))
                    Add(messages, AbTestValidationSeverity.Error,
                        $"The group name '{group.Name}' is used more than once in {label}.", i);
            }
        }

        /// <summary>
        /// The control group's list says which assets the experiment changes, and every other
        /// group replaces them row by row - so a group's list is as long as the control's, and a
        /// row pairs the control's asset with each group's at the same index.
        /// </summary>
        private void ValidateAssets(AbTestCVO test, string label, List<AbTestValidationVO> messages)
        {
            if (test.Groups.Count == 0)
                return;

            AbTestGroupCVO control = test.Groups[0];
            var listed = new HashSet<ScriptableObject>();

            if (control.Assets.Count == 0)
                messages.Add(new AbTestValidationVO
                {
                    Severity = AbTestValidationSeverity.Warning,
                    Message = $"{label} changes no asset; the game reads the player's group from RD_AbTestStatus.",
                    Scope = AbTestValidationScope.Rows
                });

            for (var row = 0; row < control.Assets.Count; row++)
            {
                ScriptableObject original = control.Assets[row];

                if (original == null)
                {
                    Add(messages, AbTestValidationSeverity.Error,
                        $"The control group of {label} has an empty slot at asset {row + 1}.", 0, row);
                    continue;
                }

                if (!listed.Add(original))
                    Add(messages, AbTestValidationSeverity.Error,
                        $"'{original.name}' is listed twice in the control group of {label}.", 0, row);

                if (CarriesSerializeReference(original.GetType()))
                    Add(messages, AbTestValidationSeverity.Warning,
                        $"'{original.name}' carries a [SerializeReference] field, which the variants of "
                        + $"{label} will not copy.", 0, row);
            }

            for (var i = 1; i < test.Groups.Count; i++)
                ValidateGroup(test.Groups[i], i, control, label, messages);
        }

        private void ValidateGroup(AbTestGroupCVO group, int column, AbTestGroupCVO control, string label,
            List<AbTestValidationVO> messages)
        {
            if (group.Assets.Count != control.Assets.Count)
                Add(messages, AbTestValidationSeverity.Error,
                    $"Group '{group.Name}' of {label} lists {group.Assets.Count} assets where the control "
                    + $"lists {control.Assets.Count}.", column);

            int rows = Math.Min(group.Assets.Count, control.Assets.Count);

            for (var row = 0; row < rows; row++)
            {
                ScriptableObject original = control.Assets[row];
                ScriptableObject variant = group.Assets[row];

                if (variant == null)
                {
                    Add(messages, AbTestValidationSeverity.Error,
                        $"Group '{group.Name}' of {label} has an empty slot at asset {row + 1}.", column, row);
                    continue;
                }

                if (original == null)
                    continue;

                if (original.GetType() != variant.GetType())
                {
                    Add(messages, AbTestValidationSeverity.Error,
                        $"Group '{group.Name}' of {label} replaces '{original.name}' with '{variant.name}', "
                        + "which is another type.", column, row);
                    continue;
                }

                if (JsonUtility.ToJson(original) == JsonUtility.ToJson(variant))
                    Add(messages, AbTestValidationSeverity.Warning,
                        $"Group '{group.Name}' of {label} replaces '{original.name}' with a variant that "
                        + "changes nothing.", column, row);
            }
        }

        /// <summary>
        /// JsonUtility skips a [SerializeReference] field without a word, so the walk looks for one
        /// anywhere Unity would serialise: the asset's own fields, and the plain classes and structs
        /// nested in them. A field pointing at another Object is a leaf - what it points at is not
        /// part of this asset.
        /// </summary>
        private bool CarriesSerializeReference(Type type) => CarriesSerializeReference(type, new HashSet<Type>());

        private bool CarriesSerializeReference(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type))
                return false;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false))
                        continue;

                    if (field.IsDefined(typeof(SerializeReference), false))
                        return true;

                    Type element = ElementType(field.FieldType);

                    if (element.IsPrimitive || element.IsEnum || element == typeof(string)
                        || typeof(Object).IsAssignableFrom(element))
                        continue;

                    if (CarriesSerializeReference(element, visited))
                        return true;
                }
            }

            return false;
        }

        private Type ElementType(Type fieldType)
        {
            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];

            return fieldType;
        }

        private void Add(List<AbTestValidationVO> messages, AbTestValidationSeverity severity, string message,
            int group = -1, int row = -1)
        {
            AbTestValidationScope scope = row >= 0 ? AbTestValidationScope.Cell
                : group >= 0 ? AbTestValidationScope.Group
                : AbTestValidationScope.Test;

            messages.Add(new AbTestValidationVO
                {Severity = severity, Message = message, Scope = scope, Group = group, Row = row});
        }
    }
}