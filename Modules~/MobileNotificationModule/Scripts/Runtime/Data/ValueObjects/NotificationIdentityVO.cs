namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// A scheduled notification's identity: the template key plus an optional tag for one
    /// instance of it (chest slot 0, 1, 2). Identifier is "key" or "key#tag" - the iOS
    /// identifier, and the data written into the notification on both platforms so the app can
    /// read back which one opened it. Id is the int Android needs, an FNV-1a hash of Identifier,
    /// so the same identity gives the same id in every session and a re-schedule replaces.
    /// </summary>
    public class NotificationIdentityVO
    {
        public const char SEPARATOR = '#';

        public string Key { get; private set; }
        public string Tag { get; private set; }
        public string Identifier { get; private set; }
        public int Id { get; private set; }

        public NotificationIdentityVO(string key, string tag) => Set(key, tag);

        /// <summary>Parses an identifier back: the part before the first '#' is the key.</summary>
        public NotificationIdentityVO(string identifier)
        {
            string text = identifier ?? string.Empty;
            int separator = text.IndexOf(SEPARATOR);

            if (separator < 0)
                Set(text, string.Empty);
            else
                Set(text.Substring(0, separator), text.Substring(separator + 1));
        }

        private void Set(string key, string tag)
        {
            Key = key ?? string.Empty;
            Tag = tag ?? string.Empty;
            Identifier = Tag.Length == 0 ? Key : Key + SEPARATOR + Tag;
            Id = Hash(Identifier);
        }

        // FNV-1a over the UTF-16 code units, kept non-negative so it reads plainly in a log.
        // string.GetHashCode is randomised per process and would cancel the wrong notification.
        private int Hash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;

                foreach (char c in text)
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return (int) (hash & 0x7fffffff);
            }
        }
    }
}
