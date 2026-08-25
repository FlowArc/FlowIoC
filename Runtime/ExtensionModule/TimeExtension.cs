using System;

namespace FlowIoC.ExtensionModule
{
    public static class TimeExtension
    {
        /// <summary>
        /// Gives the UTC time as miliseconds
        /// </summary>
        public static long GetTime()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epochStart).TotalMilliseconds;
        }

        /// <summary>
        /// Calculate the time between now and the time that is given. And returns as string in english.
        /// </summary>
        /// <param name="lastUpdate"> Last update time as miliseconds </param>
        public static string GetTimeDifference(this double lastUpdate)
        {
            if (lastUpdate != 0)
            {
                DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0);
                DateTime then = (epochStart.AddMilliseconds(lastUpdate)).ToLocalTime();
                DateTime now = DateTime.UtcNow;
                TimeSpan diff = (now - then);

                if (diff.TotalHours < 24)
                    return then.ToShortTimeString();
                else if (diff.TotalDays < 2)
                    return ((int)diff.TotalDays) + " day ago";
                else if (diff.TotalDays < 31)
                    return ((int)diff.TotalDays) + " days ago";
                else if (diff.TotalDays < 365)
                    return ((int)(diff.TotalDays / 31)) + " months ago";
                else if (diff.TotalDays / 365 < 2)
                    return ((int)(diff.TotalDays / 365)) + " year ago";
                else
                    return ((int)(diff.TotalDays / 365)) + " years ago";
            }

            return string.Empty;
        }
    }

}