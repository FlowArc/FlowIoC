namespace Modules.LocalSaveModule.Models
{
    /// <summary>
    /// What the module knows: which assets are persisted, and how to put them on disk.
    ///
    /// Restoring them is not on this surface because nothing asks for it. It happens in
    /// PostConstruct, before any other module has bound a listener that could have asked.
    /// </summary>
    public interface ILocalSaveModel
    {
        /// <summary>Writes one registered asset, by the name it was filed under on the adapter.</summary>
        void Save(string assetName);

        /// <summary>Writes every registered asset, flushing once at the end.</summary>
        void SaveAll();

#if UNITY_EDITOR
        /// <summary>
        /// Puts the assets back to what they were before the save file was read into them, so a
        /// play session leaves no diff behind. Called on quit, after the real save has been written.
        /// </summary>
        void RestoreEditorAssets();
#endif
    }
}
