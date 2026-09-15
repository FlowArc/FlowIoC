using System;
using System.Globalization;

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// A notification that goes out by itself AfterMinutes after the app is left, and is taken
    /// back when the app returns. A day is 1440; a device test can say 0.2.
    /// </summary>
    [Serializable]
    public class ReturnReminderCVO
    {
        public string Notification;
        public float AfterMinutes;

        /// <summary>
        /// The tag that keeps three reminders on one template apart: the minutes, so
        /// <c>Return#1440</c>, <c>Return#4320</c> and <c>Return#10080</c> are three scheduled
        /// notifications rather than one replacing the other.
        /// </summary>
        public string Tag => AfterMinutes.ToString(CultureInfo.InvariantCulture);
    }
}