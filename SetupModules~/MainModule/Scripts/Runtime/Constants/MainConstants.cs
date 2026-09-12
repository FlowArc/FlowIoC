namespace Modules.MainModule.Constants
{
    public static class MainConstants
    {
        public const string BOOT_SET = "Boot";
        public const string SCREENS_STEP = "Screens";
        public const string POOLS_STEP = "Pools";

        /// <summary>
        /// The pool groups the boot fills before the main screen opens. Empty in the shipped set, so
        /// the Pools step skips; a game lists its group keys here.
        /// </summary>
        public static readonly string[] BootPoolGroups = { };
    }
}
