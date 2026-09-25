#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Modules.AbTestFlowModule.Models
{
    /// <summary>
    /// Holds what each config asset looked like before a variant was written over it, and puts that
    /// back when the session ends.
    ///
    /// Editor only, and for one reason: in a build a ScriptableObject's runtime changes die with
    /// the process, but in the Editor they survive leaving play mode and land in source control as
    /// a diff on an asset nobody edited.
    ///
    /// EditorJsonUtility rather than JsonUtility here, because a snapshot wants Unity's full
    /// serialization rather than the subset JsonUtility understands. The override path itself stays
    /// on JsonUtility in both the Editor and a build, so a test proves what ships.
    /// </summary>
    public class EditorAssetSnapshots
    {
        private readonly Dictionary<ScriptableObject, string> _snapshots = new();

        /// <summary>
        /// Records the state each asset is to go back to, replacing anything captured before.
        ///
        /// That is the asset's file, not the asset in memory. A run whose restore never happened -
        /// stopped mid-boot, or a Play pressed again while the last one was still exiting - leaves a
        /// variant's values in memory with nothing marking the asset dirty, and captured from memory
        /// they became the baseline of every run after it. An asset that is dirty is the exception:
        /// that is an inspector edit the developer has not saved, and it is theirs.
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
