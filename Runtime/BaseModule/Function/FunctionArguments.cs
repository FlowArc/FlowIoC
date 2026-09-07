using System;
using FlowIoC.ConsoleModule;

namespace FlowIoC.BaseModule.Function
{
    /// <summary>
    /// Reads the parameters a caller lined up with AddParams into a function's typed Execute. A
    /// mismatch is reported rather than thrown, because it is authored at the call site and the
    /// reader needs to be told which function and which parameter to look at.
    /// </summary>
    internal static class FunctionArguments
    {
        public static bool HasArity(IFunctionBody function, object[] parameters, int expected)
        {
            int provided = parameters?.Length ?? 0;
            if (provided == expected)
                return true;

            FlowLogger.LogError(SystemLogType.Function,
                "<b><color=#FF6666>► Execute signature mismatch!</color></b>\n" +
                "<b><color=#FF6666>► Function:</color><color=#FFEFD5> " + function.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Expects:</color><color=#FFEFD5> " + expected + " parameter(s)</color></b>\n" +
                "<b><color=#FF6666>► AddParams gave:</color><color=#FFEFD5> " + provided + "</color></b>",
                function.GetType(),
                "Execute signature mismatch on " + function.GetType().Name + ": it takes " + expected +
                " parameter(s) and AddParams gave " + provided + ".");

            return false;
        }

        /// <summary>
        /// Reads one slot as <typeparamref name="T"/>. A null fills a slot whose type can hold one.
        /// </summary>
        public static bool TryFill<T>(IFunctionBody function, object[] parameters, int index, out T value)
        {
            object raw = parameters[index];

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;

            if (raw == null)
            {
                Type slotType = typeof(T);
                if (!slotType.IsValueType || Nullable.GetUnderlyingType(slotType) != null)
                    return true;

                LogSlotMismatch(function, index, slotType.Name, "null");
                return false;
            }

            LogSlotMismatch(function, index, typeof(T).Name, raw.GetType().Name);
            return false;
        }

        private static void LogSlotMismatch(IFunctionBody function, int index, string expectedTypeName, string providedTypeName)
        {
            FlowLogger.LogError(SystemLogType.Function,
                "<b><color=#FF6666>► Execute parameter mismatch!</color></b>\n" +
                "<b><color=#FF6666>► Function:</color><color=#FFEFD5> " + function.GetType().Name + "</color></b>\n" +
                "<b><color=#FF6666>► Parameter:</color><color=#FFEFD5> " + index + "</color></b>\n" +
                "<b><color=#FF6666>► Expected:</color><color=#FFEFD5> " + expectedTypeName + "</color></b>\n" +
                "<b><color=#FF6666>► Provided:</color><color=#FFEFD5> " + providedTypeName + "</color></b>",
                function.GetType(),
                "Execute parameter " + index + " of " + function.GetType().Name + " expects " + expectedTypeName +
                " and AddParams gave " + providedTypeName + ".");
        }
    }
}