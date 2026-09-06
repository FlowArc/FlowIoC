using System.IO;
using System.Linq;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.Root;
using FlowIoC.ScreenModule.RootsContexts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A generated screen context is attached to its parent module's Root by writing into that
    /// Root's prefab. The prefab is an asset on disk, so this is the one generator step that
    /// cannot be checked as a string.
    /// </summary>
    public class RootPrefabSubContextsTests
    {
        private const string Folder = "Assets/FlowIoC.Tests.Temp";
        private const string PrefabPath = Folder + "/ProbeRoot.prefab";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "FlowIoC.Tests.Temp");

            GameObject root = new GameObject("ProbeRoot");
            // ScreenServiceRoot is a concrete Root the package ships; its Awake does not run in
            // edit mode, so it is a plain RootBase with a SubContextTypes list here.
            root.AddComponent<ScreenServiceRoot>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Folder);
            if (File.Exists(Folder + ".meta")) File.Delete(Folder + ".meta");
        }

        [Test]
        public void The_context_is_added_with_auto_setup_on()
        {
            bool added = new RootPrefabSubContexts().Add(PrefabPath, "Game.MainScreenContext", "MainScreenContext");

            RootBase root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<RootBase>();
            SubContextData data = root.SubContextTypes.Single();

            Assert.IsTrue(added);
            Assert.AreEqual("Game.MainScreenContext", data.ContextFullName);
            Assert.AreEqual("MainScreenContext", data.ContextName);
            Assert.IsTrue(data.AutoSetup);
            Assert.IsFalse(data.IsTest);
        }

        [Test]
        public void Adding_the_same_context_twice_keeps_one_entry()
        {
            RootPrefabSubContexts subContexts = new RootPrefabSubContexts();
            subContexts.Add(PrefabPath, "Game.MainScreenContext", "MainScreenContext");

            bool added = subContexts.Add(PrefabPath, "Game.MainScreenContext", "MainScreenContext");

            RootBase root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<RootBase>();
            Assert.IsTrue(added);
            Assert.AreEqual(1, root.SubContextTypes.Count);
        }

        /// <summary>
        /// The entry's truth is the script asset, and the caller is the one that knows it: the
        /// generator has just written the .cs file and can load it before it has compiled, which
        /// is exactly when nothing could resolve the name to a type yet.
        /// </summary>
        [Test]
        public void The_script_the_caller_names_is_stored_on_the_entry()
        {
            // Any real script asset carries a real guid, which is the whole point of the field.
            // ScreenServiceRoot's is the one already on the probe prefab.
            Object script = MonoScript.FromMonoBehaviour(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<ScreenServiceRoot>());

            new RootPrefabSubContexts().Add(
                PrefabPath, "Game.MainScreenContext", "MainScreenContext", script);

            RootBase root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<RootBase>();

            Assert.AreSame(script, root.SubContextTypes.Single().ContextScript);
        }

        /// <summary>
        /// A caller with no script in hand still writes the entry. The name is what runtime reads,
        /// so the entry works; the inspector reports the missing script and its Resolve button
        /// fills it once the type exists.
        /// </summary>
        [Test]
        public void An_entry_added_without_a_script_still_carries_its_name()
        {
            new RootPrefabSubContexts().Add(PrefabPath, "Game.MainScreenContext", "MainScreenContext");

            RootBase root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<RootBase>();
            SubContextData data = root.SubContextTypes.Single();

            Assert.IsNull(data.ContextScript);
            Assert.AreEqual("Game.MainScreenContext", data.ContextFullName);
        }

        [Test]
        public void A_prefab_without_a_root_is_refused()
        {
            GameObject plain = new GameObject("Plain");
            string plainPath = Folder + "/Plain.prefab";
            PrefabUtility.SaveAsPrefabAsset(plain, plainPath);
            Object.DestroyImmediate(plain);

            Assert.IsFalse(new RootPrefabSubContexts().Add(plainPath, "Game.X", "X"));
        }
    }
}