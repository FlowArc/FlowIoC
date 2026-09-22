#if UNITY_EDITOR
namespace Modules.BotBarModule.BotBarTestModule.Models
{
    /// <summary>The badge count the test drives, and the last line heard.</summary>
    internal class BotBarTestModel
    {
        public int ShopBadge;
        public string LastNote = "-";
    }
}
#endif
