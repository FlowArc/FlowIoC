#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
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

        /// <summary>Records the current state of each asset, replacing anything captured before.</summary>
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
