using System;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Signals
{
    public static class SignalExtensions
    {
        /// <summary>
        /// Connects a Signal<int> to a Signal<TEnum> where TEnum is an enum type.
        /// Automatically converts the int value to the enum type.
        /// </summary>
        public static void ConnectWithConversion<TEnum>(this Signal<int> source, Signal<TEnum> target) 
            where TEnum : struct, Enum
        {
            source.AddListener((int value) => 
            {
                if (Enum.IsDefined(typeof(TEnum), value))
                {
                    TEnum enumValue = (TEnum)Enum.ToObject(typeof(TEnum), value);
                    target.Dispatch(enumValue);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Cannot convert int value {value} to {typeof(TEnum).Name}. Value is not defined in enum.");
                }
            });
        }
        
        /// <summary>
        /// Connects a Signal<TEnum> to a Signal<int> where TEnum is an enum type.
        /// Automatically converts the enum value to int.
        /// </summary>
        public static void ConnectEnumToInt<TEnum>(this Signal<TEnum> source, Signal<int> target) 
            where TEnum : struct, Enum
        {
            source.AddListener((TEnum value) => 
            {
                int intValue = Convert.ToInt32(value);
                target.Dispatch(intValue);
            });
        }
        
        /// <summary>
        /// Generic converter for any convertible types
        /// </summary>
        public static void ConnectWithConversion<TSource, TTarget>(this Signal<TSource> source, Signal<TTarget> target) 
            where TSource : IConvertible
            where TTarget : IConvertible
        {
            source.AddListener((TSource value) => 
            {
                try
                {
                    TTarget convertedValue = (TTarget)Convert.ChangeType(value, typeof(TTarget));
                    target.Dispatch(convertedValue);
                }
                catch (Exception e)
                {
                    FlowLogger.LogError(SystemLogType.Signal,
                        $"Cannot convert {typeof(TSource).Name} to {typeof(TTarget).Name}: {e.Message}");
                }
            });
        }
    }
}