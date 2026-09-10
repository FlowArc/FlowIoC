using System.Collections.Generic;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.SharedData
{
    /// <summary>
    /// One per run, owned by the RootsManager. A name is answered by the Root that filed it first,
    /// and every later filing is kept behind it rather than dropped, so the answer survives the
    /// first Root going away - a scene unloading, an additive scene bringing a filing of its own.
    /// A second filing is reported at the Root that made it: a warning when it is the same asset,
    /// an error when it is a different one, because then what a reader gets would depend on Awake
    /// order.
    /// </summary>
    public class SharedDataModel : ISharedDataModel
    {
        private readonly struct Filing
        {
            public readonly IRoot Root;
            public readonly ScriptableObject Asset;

            public Filing(IRoot root, ScriptableObject asset)
            {
                Root = root;
                Asset = asset;
            }
        }

        private readonly Dictionary<string, List<Filing>> _filings = new();

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);

        public T GetScriptable<T>(string assetName) where T : ScriptableObject
        {
            if (!_filings.TryGetValue(assetName, out List<Filing> filings) || filings.Count == 0)
            {
                FlowLogger.LogError(SystemLogType.Context,
                    "<b><color=#FF6666>► Shared asset is not filed on any Root!</color></b>\n" +
                    "<b><color=#FF6666>► Asset:</color><color=#FFEFD5> " + assetName + " (" + typeof(T).Name + ")</color></b>\n" +
                    "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null. File it once, in the Shared " +
                    "Scriptables of the Root that has it - or of a test Root, when the producer is not in the " +
                    "scene.</color></b>");
                return null;
            }

            Filing head = filings[0];
            if (head.Asset is T filed)
                return filed;

            FlowLogger.LogError(SystemLogType.Context,
                "<b><color=#FF6666>► Shared asset is filed as another type!</color></b>\n" +
                "<b><color=#FF6666>► Asset:</color><color=#FFEFD5> " + assetName + " is " + head.Asset.GetType().Name +
                ", asked for as " + typeof(T).Name + "</color></b>\n" +
                "<b><color=#FF6666>► Root:</color><color=#FFEFD5> " + head.Root.Name + "</color></b>\n" +
                "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null.</color></b>",
                context: head.Root as UnityEngine.Object);
            return null;
        }

        /// <summary>
        /// Files what a Root shares, under each name as the Root filed it. An empty entry - a name
        /// with no asset behind it - is skipped, so the reader gets the not-filed error, which
        /// names the right fix.
        /// </summary>
        internal void Register(IRoot root, IReadOnlyDictionary<string, ScriptableObject> shared)
        {
            if (root == null || shared == null)
                return;

            foreach (KeyValuePair<string, ScriptableObject> entry in shared)
            {
                if (entry.Value == null)
                    continue;

                if (!_filings.TryGetValue(entry.Key, out List<Filing> filings))
                {
                    filings = new List<Filing>();
                    _filings[entry.Key] = filings;
                }

                if (filings.Count > 0)
                    ReportSecondFiling(entry.Key, filings[0], root, entry.Value);

                filings.Add(new Filing(root, entry.Value));
            }
        }

        /// <summary>Takes back every filing this Root made. The next filing of each name, if any, answers.</summary>
        internal void UnRegister(IRoot root)
        {
            if (root == null)
                return;

            foreach (List<Filing> filings in _filings.Values)
                filings.RemoveAll(filing => filing.Root == root);
        }

        private void ReportSecondFiling(string assetName, Filing first, IRoot second, ScriptableObject asset)
        {
            if (first.Asset == asset)
            {
                FlowLogger.LogWarning(SystemLogType.Context,
                    "<b><color=#FF6666>► Shared asset is filed twice!</color></b>\n" +
                    "<b><color=#FF6666>► Asset:</color><color=#FFEFD5> " + assetName + "</color></b>\n" +
                    "<b><color=#FF6666>► Roots:</color><color=#FFEFD5> " + first.Root.Name + ", then " + second.Name + "</color></b>\n" +
                    "<b><color=#FF6666>► Result:</color><color=#FFEFD5> " + first.Root.Name + "'s filing answers. One Root " +
                    "files a shared asset; the rest read it through ISharedDataModel. Remove it from " +
                    second.Name + "'s Shared Scriptables.</color></b>");
                return;
            }

            FlowLogger.LogError(SystemLogType.Context,
                "<b><color=#FF6666>► Two different assets are shared under one name!</color></b>\n" +
                "<b><color=#FF6666>► Name:</color><color=#FFEFD5> " + assetName + "</color></b>\n" +
                "<b><color=#FF6666>► Roots:</color><color=#FFEFD5> " + first.Root.Name + " files " + first.Asset.name + "; " +
                second.Name + " files " + asset.name + "</color></b>\n" +
                "<b><color=#FF6666>► Result:</color><color=#FFEFD5> readers get " + first.Root.Name + "'s. Rename one, " +
                "or remove one.</color></b>",
                context: second as UnityEngine.Object);
        }
    }
}
