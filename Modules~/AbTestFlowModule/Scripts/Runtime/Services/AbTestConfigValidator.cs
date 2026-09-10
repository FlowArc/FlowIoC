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
    /// asset so the rules can be tested without an Editor, and called from OnValidate so a designer
    /// hears about a mistake while making it rather than at boot.
    /// </summary>
    public class AbTestConfigValidator
    {
        public List<AbTestValidationVO> Validate(List<AbTestCVO> tests)
        {
            var messages = new List<AbTestValidationVO>();
            var seen = new HashSet<string>();

            foreach (AbTestCVO test in tests)
                ValidateOne(test, seen, messages);

            return messages;
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

            if (test.Groups.Count > 0 && test.Groups[0].Overrides.Count > 0)
                Add(messages, AbTestValidationSeverity.Error,
                    $"The first group of {label} is the control group and carries no overrides.");

            if (!test.IsActive)
                Add(messages, AbTestValidationSeverity.Information, $"{label} is switched off.");
            else if (test.RolloutPercent <= 0f)
                Add(messages, AbTestValidationSeverity.Information, $"{label} rolls out to nobody.");

            ValidateOverrides(test, label, messages);
        }

        private void ValidateOverrides(AbTestCVO test, string label, List<AbTestValidationVO> messages)
        {
            var counts = new Dictionary<ScriptableObject, int>();

            for (var i = 1; i < test.Groups.Count; i++)
            {
                AbTestGroupCVO group = test.Groups[i];

                foreach (AbTestOverrideCVO pair in group.Overrides)
                {
                    if (pair.Original == null || pair.Variant == null)
                    {
                        Add(messages, AbTestValidationSeverity.Error,
                            $"Group '{group.Name}' of {label} has an override with an empty slot.");
                        continue;
                    }

                    counts.TryGetValue(pair.Original, out int count);
                    counts[pair.Original] = count + 1;

                    if (pair.Original.GetType() != pair.Variant.GetType())
                    {
                        Add(messages, AbTestValidationSeverity.Error,
                            $"Group '{group.Name}' of {label} overrides '{pair.Original.name}' with "
                            + $"'{pair.Variant.name}', which is another type.");
                        continue;
                    }

                    if (JsonUtility.ToJson(pair.Original) == JsonUtility.ToJson(pair.Variant))
                        Add(messages, AbTestValidationSeverity.Warning,
                            $"Group '{group.Name}' of {label} overrides '{pair.Original.name}' with "
                            + "a variant that changes nothing.");

                    if (CarriesSerializeReference(pair.Original.GetType()))
                        Add(messages, AbTestValidationSeverity.Warning,
                            $"'{pair.Original.name}' carries a [SerializeReference] field, which the "
                            + $"override of group '{group.Name}' of {label} will not copy.");
                }
            }

            int arms = test.Groups.Count - 1;

            foreach (KeyValuePair<ScriptableObject, int> entry in counts)
            {
                if (entry.Value < arms)
                    Add(messages, AbTestValidationSeverity.Warning,
                        $"'{entry.Key.name}' is overridden in {entry.Value} of {arms} groups of "
                        + $"{label}, so it is not in every group.");
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

        private void Add(List<AbTestValidationVO> messages, AbTestValidationSeverity severity, string message) =>
            messages.Add(new AbTestValidationVO {Severity = severity, Message = message});
    }
}
