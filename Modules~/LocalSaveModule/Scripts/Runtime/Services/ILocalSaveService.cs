namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// Everything the module does to a file, behind one seam. It knows no game type and touches
    /// no scene object: it answers the key and the object it is handed, which is what makes it a
    /// Service rather than a System. The steps a game binds to have its data written sit under
    /// <see cref="Commands"/>, one file each beside this one.
    /// </summary>
    public partial interface ILocalSaveService
    {
        /// <summary>
        /// Reads the save file into memory once. Every load and save afterwards is served from
        /// there, so a startup that restores a dozen assets opens the file once rather than
        /// twelve times. The password is the one the file is encrypted with; null or empty means
        /// a plain file, and a file that was written plain is read whatever the password says.
        /// </summary>
        void BeginSession(string password);

        /// <summary>Whether anything was ever written under this key.</summary>
        bool Has(string key);

        /// <summary>
        /// Overwrites the fields of an object that already exists, rather than returning a new
        /// one. This is what lets the module restore a ScriptableObject without knowing its type.
        /// </summary>
        void LoadInto(string key, object target);

        /// <summary>Writes a value into the in-memory copy of the file.</summary>
        void Save(string key, object value);

        /// <summary>Writes the in-memory copy back to disk. Nothing reaches storage until this runs.</summary>
        void Flush();

        /// <summary>
        /// The steps a game binds in a sequence of its own, after the step that changed the data.
        /// They sit inside the interface so that the one name a game knows - the Service - is also
        /// where its steps are found, and the flow reads from the Context: what changed, and that
        /// it was written. Each step is a file of its own, <c>ILocalSaveService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}