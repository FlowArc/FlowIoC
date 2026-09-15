#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
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
        /// Records the current state of each asset, replacing anything captured before.
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
                    _snapshots[asset] = EditorJsonUtility.ToJson(asset);
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
    }
}

#endif
