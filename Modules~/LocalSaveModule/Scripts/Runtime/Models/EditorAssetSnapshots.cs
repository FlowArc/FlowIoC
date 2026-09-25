#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modules.LocalSaveModule.Models
{
    /// <summary>
    /// Holds what each persisted asset looked like before the save file was read into it, and puts
    /// that back when the session ends.
    ///
    /// Editor only, and for one reason: in a build a ScriptableObject's runtime changes die with
    /// the process, but in the Editor they survive leaving play mode and land in source control as
    /// a diff on an asset nobody edited. Restoring means the save system can run normally in the
    /// Editor and still be tested, instead of being switched off to keep the assets clean.
    /// </summary>
    public class EditorAssetSnapshots
    {
        private readonly Dictionary<ScriptableObject, string> _snapshots = new();

        /// <summary>
        /// Records the state each asset is to go back to, replacing anything captured before.
        ///
        /// That is the asset's file, not the asset in memory. A run whose restore never happened -
        /// stopped mid-boot, or a Play pressed again while the last one was still exiting - leaves
        /// the save file's values in memory with nothing marking the asset dirty, and captured from
        /// memory they became the baseline of every run after it. An asset that is dirty is the
        /// exception: that is an inspector edit the developer has not saved, and it is theirs.
        ///
        /// EditorJsonUtility rather than JsonUtility, because it captures [SerializeReference]
        /// fields and Unity's full serialization rather than the subset JsonUtility understands.
        /// </summary>
        public void Capture(IEnumerable<ScriptableObject> assets)
        {
            _snapshots.Clear();

            foreach (ScriptableObject asset in assets)
            {
                if (asset != null)
                    _snapshots[asset] = Baseline(asset);
            }
        }

        /// <summary>Writes every recorded state back over the asset it came from.</summary>
        public void Restore()
        {
            foreach (KeyValuePair<ScriptableObject, string> snapshot in _snapshots)
            {
                if (snapshot.Key != null)
                    EditorJsonUtility.FromJsonOverwrite(snapshot.Value, snapshot.Key);
            }
        }

        /// <summary>
        /// The file is read into a copy the asset database does not know about, so the asset in
        /// memory - and every reference to it from the scene - is left as it is.
        /// </summary>
        private static string Baseline(ScriptableObject asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path) || EditorUtility.IsDirty(asset))
                return EditorJsonUtility.ToJson(asset);

            Object[] copies = InternalEditorUtility.LoadSerializedFileAndForget(path);

            try
            {
                foreach (Object copy in copies)
                {
                    if (copy != null && copy.GetType() == asset.GetType())
                        return EditorJsonUtility.ToJson(copy);
                }

                return EditorJsonUtility.ToJson(asset);
            }
            finally
            {
                foreach (Object copy in copies)
                {
                    if (copy != null)
                        Object.DestroyImmediate(copy);
                }
            }
        }
    }
}

#endif
