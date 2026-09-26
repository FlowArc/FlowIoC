#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Root;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Which settings class an entry of a context carries: the TSettings of the
    /// ISubContextConfigurable&lt;TSettings&gt; the context implements, or null when it takes none.
    /// </summary>
    internal class SubContextSettingsTypes
    {
        internal Type For(Type contextType)
        {
            if (contextType == null)
                return null;

            foreach (Type contract in contextType.GetInterfaces())
            {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ISubContextConfigurable<>))
                    return contract.GetGenericArguments()[0];
            }

            return null;
        }

        /// <summary>The settings class's name without its suffix: PoolSubContextSettingsCVO is "Pool".</summary>
        internal string Title(Type settingsType)
            => UnityEditor.ObjectNames.NicifyVariableName(settingsType.Name.Replace("SubContextSettingsCVO", string.Empty));

        /// <summary>A fresh instance of the context's settings, or null when it takes none.</summary>
        internal SubContextSettingsCVO NewFor(Type contextType)
        {
            Type settingsType = For(contextType);

            return settingsType == null ? null : (SubContextSettingsCVO) Activator.CreateInstance(settingsType);
        }
    }
}
#endif
