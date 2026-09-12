using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.SharedData
{
    /// <summary>
    /// One per run, owned by the RootsManager. A name is answered by the Root that filed it first,
    /// and every later filing is kept behind it rather than dropped, so the answer survives the
    /// first Root going away - a scene unloading, an additive scene bringing a filing of its own.
    /// A second filing is reported at the Root that made it: a warning when it is the same object,
    /// an error when it is a different one, because then what a reader gets would depend on Awake
    /// order.
    ///
    /// Assets and scene components are filed apart, one registry each, so a name in one says
    /// nothing about the other - the adapter keeps them in separate slots for the same reason.
    /// </summary>
    public class SharedDataModel : ISharedDataModel
    {
        [ShowInModelViewer] private readonly Filings _scriptables = new("asset", "Shared Scriptables");
        [ShowInModelViewer] private readonly Filings _monoBehaviours = new("component", "Shared Monos");

        public T GetScriptable<T>() where T : ScriptableObject => GetScriptable<T>(typeof(T).Name);

        public T GetScriptable<T>(string assetName) where T : ScriptableObject => _scriptables.Get<T>(assetName);

        public T GetMonoBehaviour<T>() where T : MonoBehaviour => GetMonoBehaviour<T>(typeof(T).Name);

        public T GetMonoBehaviour<T>(string componentName) where T : MonoBehaviour => _monoBehaviours.Get<T>(componentName);

        /// <summary>
        /// Files what a Root shares, under each name as the Root filed it. Either map may be null -
        /// a Root with no adapter, or an empty slot - and an empty entry, a name with nothing behind
        /// it, is skipped so the reader gets the not-filed error, which names the right fix.
        /// </summary>
        internal void Register(IRoot root,
            IReadOnlyDictionary<string, ScriptableObject> scriptables,
            IReadOnlyDictionary<string, MonoBehaviour> monoBehaviours)
        {
            if (root == null)
                return;

            if (scriptables != null)
            {
                foreach (KeyValuePair<string, ScriptableObject> entry in scriptables)
                    _scriptables.File(root, entry.Key, entry.Value);
            }

            if (monoBehaviours != null)
            {
                foreach (KeyValuePair<string, MonoBehaviour> entry in monoBehaviours)
                    _monoBehaviours.File(root, entry.Key, entry.Value);
            }
        }

        /// <summary>Takes back every filing this Root made. The next filing of each name, if any, answers.</summary>
        internal void UnRegister(IRoot root)
        {
            if (root == null)
                return;

            _scriptables.Withdraw(root);
            _monoBehaviours.Withdraw(root);
        }

        /// <summary>
        /// One registry: the filings of one kind of object, by name, each name keeping every Root
        /// that filed it in the order they came. The two words it is built with are what its
        /// reports say - which kind of thing is missing, and which slot of the adapter to fix.
        /// </summary>
        private class Filings
        {
            private readonly struct Filing
            {
                public readonly IRoot Root;
                public readonly Object Entry;

                public Filing(IRoot root, Object entry)
                {
                    Root = root;
                    Entry = entry;
                }
            }

            private readonly string _kind;
            private readonly string _slot;
            [ShowInModelViewer] private readonly Dictionary<string, List<Filing>> _byName = new();

            public Filings(string kind, string slot)
            {
                _kind = kind;
                _slot = slot;
            }

            public T Get<T>(string name) where T : Object
            {
                if (!_byName.TryGetValue(name, out List<Filing> filings) || filings.Count == 0)
                {
                    FlowLogger.LogError(SystemLogType.Context,
                        "<b><color=#FF6666>► Shared " + _kind + " is not filed on any Root!</color></b>\n" +
                        "<b><color=#FF6666>► " + Capitalised(_kind) + ":</color><color=#FFEFD5> " + name + " (" + typeof(T).Name + ")</color></b>\n" +
                        "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null. File it once, in the " + _slot +
                        " of the Root that has it - or of a test Root, when the producer is not in the scene.</color></b>");
                    return null;
                }

                Filing head = filings[0];
                if (head.Entry is T filed)
                    return filed;

                FlowLogger.LogError(SystemLogType.Context,
                    "<b><color=#FF6666>► Shared " + _kind + " is filed as another type!</color></b>\n" +
                    "<b><color=#FF6666>► " + Capitalised(_kind) + ":</color><color=#FFEFD5> " + name + " is " + head.Entry.GetType().Name +
                    ", asked for as " + typeof(T).Name + "</color></b>\n" +
                    "<b><color=#FF6666>► Root:</color><color=#FFEFD5> " + head.Root.Name + "</color></b>\n" +
                    "<b><color=#FF6666>► Result:</color><color=#FFEFD5> the caller gets null.</color></b>",
                    context: head.Root as Object);
                return null;
            }

            public void File(IRoot root, string name, Object entry)
            {
                if (entry == null)
                    return;

                if (!_byName.TryGetValue(name, out List<Filing> filings))
                {
                    filings = new List<Filing>();
                    _byName[name] = filings;
                }

                if (filings.Count > 0)
                    ReportSecondFiling(name, filings[0], root, entry);

                filings.Add(new Filing(root, entry));
            }

            public void Withdraw(IRoot root)
            {
                foreach (List<Filing> filings in _byName.Values)
                    filings.RemoveAll(filing => filing.Root == root);
            }

            private void ReportSecondFiling(string name, Filing first, IRoot second, Object entry)
            {
                if (first.Entry == entry)
                {
                    FlowLogger.LogWarning(SystemLogType.Context,
                        "<b><color=#FF6666>► Shared " + _kind + " is filed twice!</color></b>\n" +
                        "<b><color=#FF6666>► " + Capitalised(_kind) + ":</color><color=#FFEFD5> " + name + "</color></b>\n" +
                        "<b><color=#FF6666>► Roots:</color><color=#FFEFD5> " + first.Root.Name + ", then " + second.Name + "</color></b>\n" +
                        "<b><color=#FF6666>► Result:</color><color=#FFEFD5> " + first.Root.Name + "'s filing answers. One Root " +
                        "files a shared " + _kind + "; the rest read it through ISharedDataModel. Remove it from " +
                        second.Name + "'s " + _slot + ".</color></b>");
                    return;
                }

                FlowLogger.LogError(SystemLogType.Context,
                    "<b><color=#FF6666>► Two different " + _kind + "s are shared under one name!</color></b>\n" +
                    "<b><color=#FF6666>► Name:</color><color=#FFEFD5> " + name + "</color></b>\n" +
                    "<b><color=#FF6666>► Roots:</color><color=#FFEFD5> " + first.Root.Name + " files " + first.Entry.name + "; " +
                    second.Name + " files " + entry.name + "</color></b>\n" +
                    "<b><color=#FF6666>► Result:</color><color=#FFEFD5> readers get " + first.Root.Name + "'s. Rename one, " +
                    "or remove one.</color></b>",
                    context: second as Object);
            }

            private string Capitalised(string word) => char.ToUpperInvariant(word[0]) + word.Substring(1);
        }
    }
}