using System;
using System.Collections.Generic;
using System.Reflection;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using Modules.AudioModule.Shared;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Every key the project declares: the static AudioKey fields of the classes the modules nest
    /// in AudioKey. What the bank check compares the banks against, and what the Inspector lists.
    /// </summary>
    internal class ListDeclaredKeysFunction : FunctionReturn<IReadOnlyList<AudioKey>>
    {
        public override IReadOnlyList<AudioKey> Execute()
        {
            var keys = new List<AudioKey>();

            foreach (Type nested in typeof(AudioKey).GetNestedTypes(BindingFlags.Public))
            {
                foreach (FieldInfo field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(AudioKey) && field.GetValue(null) is AudioKey key && !key.IsEmpty)
                        keys.Add(key);
                }
            }

            return keys;
        }
    }
}
