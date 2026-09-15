#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Modules.AssetDeliveryModule.Editor
{
    /// <summary>
    /// A pack's name on each store. Google accepts a letter first and letters, digits and
    /// underscores after; Unity's Android build renames a group that breaks that rule, and the
    /// name it chose is read back from the file it writes rather than guessed. iOS takes the
    /// group name sanitised the same way, and two groups that sanitise to one name are reported.
    /// </summary>
    internal class AssetPackNames
    {
        private readonly Regex _compliant = new("^[A-Za-z][A-Za-z0-9_]*$");

        public bool IsGoogleCompliant(string name) => !string.IsNullOrEmpty(name) && _compliant.IsMatch(name);

        public string Sanitize(string group)
        {
            var builder = new StringBuilder(group.Length + 5);

            foreach (char c in group)
                builder.Append((char.IsLetterOrDigit(c) && c < 128) || c == '_' ? c : '_');

            string name = builder.ToString();
            return name.Length == 0 || !char.IsLetter(name[0]) ? "Pack_" + name : name;
        }

        public IReadOnlyList<string> Collisions(IEnumerable<string> groups)
        {
            var seen = new HashSet<string>();
            var collisions = new List<string>();

            foreach (string group in groups)
            {
                string name = Sanitize(group);

                if (!seen.Add(name) && !collisions.Contains(name))
                    collisions.Add(name);
            }

            return collisions;
        }

        /// <summary>
        /// Bundle name to pack name, as Unity wrote them. Unity lists a bundle without its
        /// extension, so the key is the file name without one, and a caller asks with the same.
        /// </summary>
        public IReadOnlyDictionary<string, string> AndroidBundleToPack(string customAssetPacksDataJson)
        {
            var map = new Dictionary<string, string>();
            CustomAssetPacksDataMirror data = JsonUtility.FromJson<CustomAssetPacksDataMirror>(customAssetPacksDataJson);

            if (data?.Entries == null) return map;

            foreach (CustomAssetPackEntryMirror entry in data.Entries)
                foreach (string bundle in entry.AssetBundles)
                    map[System.IO.Path.GetFileNameWithoutExtension(bundle)] = entry.m_AssetPackName;

            return map;
        }

        /// <summary>The shape of Unity's CustomAssetPacksData.json, field for field, so it reads without the package's internal type.</summary>
        [Serializable]
        private class CustomAssetPacksDataMirror
        {
            public List<CustomAssetPackEntryMirror> Entries;
        }

        [Serializable]
        private class CustomAssetPackEntryMirror
        {
            public string m_AssetPackName;
            public int m_DeliveryType;
            public List<string> AssetBundles;
        }
    }
}
#endif
