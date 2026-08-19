using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;

namespace FlowIoC.ScreenModule.Extensions
{
    public static class ScreenDataExtensions
    {
        /// <summary>
        /// 🔹 Belirli bir flag'i ekleme (Add)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static void AddState(this ScreenVO data, ScreenState value)
        {
            data.State = data.State | value;
        }

        /// <summary>
        /// 🔹 Belirli bir flag'i çıkarma (Remove)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static void RemoveState(this ScreenVO data, ScreenState value)
        {
            data.State = data.State &~ value;
        }
        /// <summary>
        /// 🔹 Belirli bir flag'i içeriyor mu? (Has)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static bool HasState(this ScreenVO data, ScreenState value)
        {
            // data.State = data.State & value;
            return data.State.HasFlag(value);
        }
        
        /// <summary>
        /// 🔹 Belirli bir flag'i toggle yapma (varsa çıkar, yoksa ekle)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static void ToggleState(this ScreenVO data, ScreenState value)
        {
            data.State = data.State ^ value;
        }
    }
}