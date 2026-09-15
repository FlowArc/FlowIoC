namespace Modules.LoadingModule.LoadingScreenModule.Constants
{
    internal static class LoadingScreenConstants
    {
        /// <summary>
        /// The address of the art behind the bar. The prefab comes from Resources with this same
        /// art bundled on its Background, so the screen is on stage before Addressables has
        /// initialised; the addressable copy replaces it once the load lands, which is what lets a
        /// remote catalogue change the art between releases without a build. A game keeps the
        /// address and replaces the asset - in <c>Art/</c>, where the installer registers it.
        /// </summary>
        public const string BACKGROUND_KEY = "T_LoadingBackground";
    }
}
