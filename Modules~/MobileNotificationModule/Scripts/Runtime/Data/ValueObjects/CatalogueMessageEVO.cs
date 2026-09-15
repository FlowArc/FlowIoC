#if UNITY_EDITOR

namespace Modules.MobileNotificationModule.Data.ValueObjects
{
    /// <summary>
    /// One thing the panel has to say about the catalogue or the project settings: what it is
    /// about (a template key, a channel key, a reminder's line, or empty for the whole asset),
    /// the sentence, and whether it stops a notification from working or only warns.
    /// </summary>
    public class CatalogueMessageEVO
    {
        public string About;
        public string Text;
        public bool IsError;
    }
}

#endif
