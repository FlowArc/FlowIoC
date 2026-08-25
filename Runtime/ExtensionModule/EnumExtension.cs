using System;
using System.Collections.Generic;

namespace FlowIoC.ExtensionModule
{
    public static class EnumExtension
    {
        /// <summary>
        /// Get true flags of an Enum
        /// </summary>
        /// <param name="flags"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetUniqueFlags<T>(this T flags) where T : Enum  
        {
            foreach (Enum value in Enum.GetValues(flags.GetType()))
                if (flags.HasFlag(value))
                    yield return (T)value;
        }
    }

}