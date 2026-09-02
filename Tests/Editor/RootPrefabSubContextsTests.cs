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
