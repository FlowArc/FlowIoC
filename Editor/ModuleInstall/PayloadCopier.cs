#if UNITY_EDITOR

using System.IO;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// Copies a shipped module's tree into the project, and puts back the folders git never
    /// stored. A module's skeleton - an empty Entities, an empty Editor - travels as the `.meta`
    /// Unity wrote for the folder, marked `folderAsset: yes`, because git keeps no empty directory.
    /// Unity recreates the folder from that meta on import, with a warning for each: a hundred of
    /// them after the setup set lands, none of them about anything wrong. Creating the folder
    /// here, before the import sees the meta, keeps the skeleton and loses the noise.
    /// </summary>
    internal class PayloadCopier
    {
        private const string META = ".meta";
        private const string FOLDER_MARK = "folderAsset: yes";

        internal void CopyTree(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                string copy = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, copy, false);

                if (IsFolderMeta(file))
                    Directory.CreateDirectory(copy.Substring(0, copy.Length - META.Length));
            }

            foreach (string directory in Directory.GetDirectories(source))
                CopyTree(directory, Path.Combine(target, Path.GetFileName(directory)));
        }

        /// <summary>
        /// Whether a file is the meta of a folder. Read from the text rather than assumed from the
        /// name: a file's meta and a folder's differ only in what they say inside.
        /// </summary>
        private static bool IsFolderMeta(string path)
        {
            if (!path.EndsWith(META, System.StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                return File.ReadAllText(path).Contains(FOLDER_MARK);
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}

#endif
