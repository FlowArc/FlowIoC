using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.CodeGenerator.Menus.Module.RenameModule;
using FlowIoC.Editor.Config.ModuleConfig;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The plan is the whole rename, computed from the tree and a fake disk before anything is
    /// written: which modules change, what their assemblies, namespaces, classes, assets and
    /// channels become, what is kept and why, and what blocks the press.
    /// </summary>
    public class ModuleRenamePlanTests
    {
        private const string ROOT = "D:/p/Assets/Modules";
        private const string COUNTER = ROOT + "/CounterModule";
        private const string TEST = COUNTER + "/zTestModules/CounterTestModule";

        private Dictionary<string, string> _files;
        private Dictionary<string, List<TypeHomeEVO>> _types;
        private List<string> _assemblies;
        private List<string> _shipped;

        [SetUp]
        public void FreshDisk()
        {
            _files = new Dictionary<string, string>
            {
                [COUNTER + "/Modules.Counter.asmdef"] = "{\"name\": \"Modules.Counter\"}",
                [COUNTER + "/MODULE.md"] = "# CounterModule",
                [COUNTER + "/Scripts/Signals/Modules.Counter.Signals.asmdef"] = "{\"name\": \"Modules.Counter.Signals\"}",
                [COUNTER + "/Scripts/Signals/CounterServiceSignals.cs"] =
                    "public class CounterServiceSignals {} public class CounterServiceSignalsIncoming {} public class CounterServiceSignalsOutgoing {}",
                [COUNTER + "/Scripts/Runtime/RootsContexts/CounterServiceRoot.cs"] = "public class CounterServiceRoot : Root<CounterServiceContext> {}",
                [COUNTER + "/Scripts/Runtime/RootsContexts/CounterServiceContext.cs"] = "public class CounterServiceContext : Context {}",
                [COUNTER + "/Scripts/Runtime/Signals/CounterServiceInternalSignals.cs"] = "internal class CounterServiceInternalSignals {}",
                [COUNTER + "/Scripts/Runtime/Models/CounterModel.cs"] = "public class CounterModel {}",
                [COUNTER + "/Prefabs/CounterServiceRoot.prefab"] = "",
                [COUNTER + "/Prefabs/Other.prefab"] = "",
                [TEST + "/Modules.Counter.Test.asmdef"] = "{\"name\": \"Modules.Counter.Test\"}",
                [TEST + "/Scripts/Runtime/RootsContexts/CounterTestRoot.cs"] = "public class CounterTestRoot {}",
                [TEST + "/Scripts/Runtime/RootsContexts/CounterTestContext.cs"] = "public class CounterTestContext {}",
                [TEST + "/Scenes/CounterTestScene.unity"] = "",
                [ROOT + "/OtherModule/Modules.Other.asmdef"] = "{\"name\": \"Modules.Other\"}"
            };

            _types = new Dictionary<string, List<TypeHomeEVO>>();
            _assemblies = null;
            _shipped = null;
        }

        private RenameProjectLookups Lookups()
        {
            var layout = new Dictionary<FolderEVO.FolderType, string>
            {
                [FolderEVO.FolderType.RootsAndContexts] = "/Scripts/Runtime/RootsContexts",
                [FolderEVO.FolderType.Signals] = "/Scripts/Runtime/Signals",
                [FolderEVO.FolderType.PublicSignals] = "/Scripts/Signals",
                [FolderEVO.FolderType.Shared] = "/Scripts/Shared",
                [FolderEVO.FolderType.ViewsAndMediators] = "/Scripts/Runtime/ViewsMediators",
                [FolderEVO.FolderType.Prefabs] = "/Prefabs",
                [FolderEVO.FolderType.Scenes] = "/Scenes",
                [FolderEVO.FolderType.Resources] = "/Resources"
            };

            bool Exists(string folder) => _files.Keys.Any(path => path.StartsWith(folder + "/", StringComparison.Ordinal));

            return new RenameProjectLookups
            {
                FolderOf = (modulePath, kind, type) =>
                    layout.TryGetValue(type, out string tail) && Exists(modulePath + tail) ? modulePath + tail : null,
                FilesIn = folder => _files.Keys
                    .Where(path => path.StartsWith(folder + "/", StringComparison.Ordinal))
                    .Where(path => !path.Substring(folder.Length + 1).Contains("/"))
                    .ToList(),
                ReadText = path => _files[path],
                DirectoryExists = Exists,
                AllAssemblyNames = () => _assemblies ?? _files.Keys
                    .Where(path => path.EndsWith(".asmdef"))
                    .Select(Path.GetFileNameWithoutExtension)
                    .Concat(new[] {"FlowIoC"})
                    .ToList(),
                TypesNamed = name => _types.TryGetValue(name, out List<TypeHomeEVO> homes) ? homes : new List<TypeHomeEVO>(),
                ShippedAssemblyNames = () => _shipped ?? new List<string>()
            };
        }

        private static ModulePickEVO Pick(string name, ModuleKind kind, string parent, string path) =>
            new ModulePickEVO {Name = name, Kind = kind, ParentName = parent, Path = path};

        private static ModuleTreeRowEVO<ModulePickEVO> Picked(string name, params ModulePickEVO[] picks) =>
            new ModuleTree().Build(picks.ToList()).First(row => row.Row.Name == name);

        private static ModuleTreeRowEVO<ModulePickEVO> Counter() => Picked("CounterModule",
            Pick("CounterModule", ModuleKind.Main, null, COUNTER),
            Pick("CounterTestModule", ModuleKind.Test, "CounterModule", TEST),
            Pick("OtherModule", ModuleKind.Main, null, ROOT + "/OtherModule"));

        private ModuleRenamePlanEVO Plan(string stem = "Timer", Func<string, bool> ticked = null) =>
            new ModuleRenamePlan(Lookups()).Build(Counter(), stem, ticked ?? (_ => true));

        [Test]
        public void The_picked_module_and_its_carrier_change_name_stem_and_namespace()
        {
            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.CanRun, string.Join("; ", plan.Blockers));
            CollectionAssert.AreEqual(new[] {"CounterModule", "CounterTestModule"}, plan.Modules.Select(m => m.OldName));
            CollectionAssert.AreEqual(new[] {"TimerModule", "TimerTestModule"}, plan.Modules.Select(m => m.NewName));
            Assert.IsTrue(plan.Picked.IsPicked);
            Assert.AreEqual("Modules.CounterModule", plan.Modules[0].OldNamespace);
            Assert.AreEqual("Modules.TimerModule", plan.Modules[0].NewNamespace);
            Assert.AreEqual("Modules.CounterModule.CounterTestModule", plan.Modules[1].OldNamespace);
            Assert.AreEqual("Modules.TimerModule.TimerTestModule", plan.Modules[1].NewNamespace);
            Assert.AreEqual("CounterTest", plan.Modules[1].OldStem);
            Assert.AreEqual("TimerTest", plan.Modules[1].NewStem);
        }

        [Test]
        public void The_assemblies_are_read_off_the_asmdefs_and_renamed_where_the_folder_names_them()
        {
            ModuleRenamePlanEVO plan = Plan();

            CollectionAssert.AreEquivalent(
                new[] {"Modules.Counter", "Modules.Counter.Signals", "Modules.Counter.Test"},
                plan.Assemblies.Select(a => a.OldName));
            Assert.AreEqual("Modules.Timer.Signals", plan.Assemblies.Single(a => a.OldName == "Modules.Counter.Signals").NewName);
            Assert.AreEqual(COUNTER + "/Scripts/Signals/Modules.Counter.Signals.asmdef",
                plan.Assemblies.Single(a => a.OldName == "Modules.Counter.Signals").AsmdefPath);
        }

        [Test]
        public void An_asmdef_not_named_after_the_module_is_kept_and_says_so()
        {
            _files.Remove(COUNTER + "/Modules.Counter.asmdef");
            _files[COUNTER + "/Modules.Tick.asmdef"] = "{\"name\": \"Modules.Tick\"}";

            ModuleRenamePlanEVO plan = Plan();

            Assert.IsFalse(plan.Assemblies.Any(a => a.OldName == "Modules.Tick"));
            Assert.IsTrue(plan.Kept.Any(line => line.Contains("Modules.Tick")));
        }

        [Test]
        public void The_generated_set_is_the_named_files_of_both_modules_and_a_model_is_not_in_it()
        {
            ModuleRenamePlanEVO plan = Plan();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "CounterServiceSignals.cs", "CounterServiceRoot.cs", "CounterServiceContext.cs",
                    "CounterServiceInternalSignals.cs", "CounterTestRoot.cs", "CounterTestContext.cs"
                },
                plan.Classes.Select(c => Path.GetFileName(c.Path)));
            Assert.AreEqual("TimerTestRoot",
                plan.Classes.Single(c => c.Path.EndsWith("CounterTestRoot.cs")).Identifiers.Single().New);
            Assert.AreEqual(3, plan.Classes.Single(c => c.Path.EndsWith("CounterServiceSignals.cs")).Identifiers.Count);
        }

        [Test]
        public void Assets_in_the_own_folders_that_carry_the_stem_follow_and_the_others_do_not()
        {
            ModuleRenamePlanEVO plan = Plan();

            CollectionAssert.AreEquivalent(
                new[] {"TimerServiceRoot.prefab", "TimerTestScene.unity"},
                plan.Assets.Select(a => Path.GetFileName(a.NewPath)));
        }

        [Test]
        public void Only_the_modules_with_a_channel_get_a_channel_line()
        {
            ModuleRenamePlanEVO plan = Plan();

            Assert.AreEqual(1, plan.Channels.Count);
            Assert.AreEqual("CounterModule", plan.Channels[0].OldName);
            Assert.AreEqual("TimerModule", plan.Channels[0].NewName);
        }

        [Test]
        public void The_settings_lines_name_every_renamed_assembly()
        {
            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.SettingsFiles.Any(line => line.StartsWith("Modules.Counter.Test.csproj.DotSettings")));
            Assert.IsTrue(plan.SettingsFiles.Any(line => line.Contains("Modules.Timer.csproj.DotSettings")));
        }

        [Test]
        public void An_unticked_carrier_keeps_its_name_and_its_settings_file_is_rewritten_in_place()
        {
            ModuleRenamePlanEVO plan = Plan("Timer", name => name != "CounterTestModule");

            Assert.AreEqual(1, plan.Modules.Count);
            Assert.AreEqual(1, plan.Carriers.Count);
            Assert.IsFalse(plan.Carriers[0].Follows);
            Assert.IsTrue(plan.Kept.Any(line => line.StartsWith("CounterTestModule")));
            Assert.IsTrue(plan.SettingsFiles.Any(line => line.StartsWith("Modules.Counter.Test.csproj.DotSettings") && line.Contains("rewritten")));
            Assert.IsFalse(plan.Assemblies.Any(a => a.OldName == "Modules.Counter.Test"));
        }

        [Test]
        public void An_invalid_stem_or_the_same_name_blocks_and_plans_nothing()
        {
            Assert.AreEqual(1, Plan("").Blockers.Count);
            Assert.IsEmpty(Plan("").Modules);
            Assert.AreEqual(1, Plan("Counter").Blockers.Count);
            Assert.IsEmpty(Plan("Counter").Modules);
        }

        [Test]
        public void A_folder_already_in_the_way_blocks()
        {
            _files[ROOT + "/TimerModule/Modules.Timer2.asmdef"] = "{}";

            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.Blockers.Any(line => line.Contains("TimerModule")), string.Join("; ", plan.Blockers));
        }

        [Test]
        public void An_assembly_name_already_declared_blocks()
        {
            _assemblies = new List<string> {"FlowIoC", "Modules.Counter", "Modules.Timer.Signals"};

            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.Blockers.Any(line => line.Contains("Modules.Timer.Signals")));
        }

        [Test]
        public void A_module_the_package_ships_warns_that_the_installer_will_offer_it_again()
        {
            _shipped = new List<string> {"Modules.Counter", "Modules.AbTestFlow"};

            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.CanRun);
            Assert.AreEqual(1, plan.Warnings.Count(line => line.StartsWith("Modules.Counter is the assembly")));
        }

        [Test]
        public void A_class_whose_name_exists_elsewhere_is_skipped_with_a_warning_and_its_file_keeps_its_name()
        {
            _types["CounterServiceRoot"] = new List<TypeHomeEVO>
            {
                new TypeHomeEVO {FullName = "Modules.CounterModule.RootsContexts.CounterServiceRoot", AssemblyName = "Modules.Counter"},
                new TypeHomeEVO {FullName = "Legacy.CounterServiceRoot", AssemblyName = "Legacy"}
            };

            ModuleRenamePlanEVO plan = Plan();
            ClassRenameEVO root = plan.Classes.Single(c => c.Path.EndsWith("CounterServiceRoot.cs"));

            Assert.IsTrue(plan.CanRun);
            Assert.IsEmpty(root.Identifiers);
            Assert.IsNull(root.NewPath);
            Assert.IsTrue(plan.Warnings.Any(line => line.Contains("Legacy.CounterServiceRoot")));
        }

        [Test]
        public void A_new_class_name_taken_in_the_module_blocks_and_taken_elsewhere_only_warns()
        {
            _types["TimerServiceRoot"] = new List<TypeHomeEVO>
            {
                new TypeHomeEVO {FullName = "Modules.CounterModule.X.TimerServiceRoot", AssemblyName = "Modules.Counter"}
            };
            _types["TimerServiceContext"] = new List<TypeHomeEVO>
            {
                new TypeHomeEVO {FullName = "Elsewhere.TimerServiceContext", AssemblyName = "Modules.Other"}
            };

            ModuleRenamePlanEVO plan = Plan();

            Assert.IsTrue(plan.Blockers.Any(line => line.Contains("TimerServiceRoot")));
            Assert.IsTrue(plan.Warnings.Any(line => line.Contains("Elsewhere.TimerServiceContext")));
        }

        [Test]
        public void A_screen_module_with_its_prefab_plans_the_address_and_without_it_keeps_the_address()
        {
            const string gameplay = ROOT + "/GameplayModule";
            const string screen = gameplay + "/zScreenModules/GameplayScreenModule";
            _files[gameplay + "/Modules.Gameplay.asmdef"] = "{}";
            _files[screen + "/Modules.Gameplay.Screen.asmdef"] = "{}";
            _files[screen + "/Prefabs/GameplayScreen.prefab"] = "";

            ModuleTreeRowEVO<ModulePickEVO> picked = Picked("GameplayScreenModule",
                Pick("GameplayModule", ModuleKind.Main, null, gameplay),
                Pick("GameplayScreenModule", ModuleKind.Screen, "GameplayModule", screen));

            ModuleRenamePlanEVO plan = new ModuleRenamePlan(Lookups()).Build(picked, "Hud", _ => true);

            ScreenAddressRenameEVO address = plan.ScreenAddresses.Single();
            Assert.AreEqual("GameplayScreen", address.OldAddress);
            Assert.AreEqual("HudScreen", address.NewAddress);
            Assert.AreEqual("Local_Screen-Gameplay", address.OldGroup);
            Assert.AreEqual("Local_Screen-Hud", address.NewGroup);
            Assert.IsTrue(address.NewPrefabPath.Replace('\\', '/').EndsWith("/Prefabs/HudScreen.prefab"));
            Assert.AreEqual("Modules.Hud.Screen", plan.Assemblies.Single().NewName);
            Assert.AreEqual("Modules.GameplayModule.HudScreenModule", plan.Picked.NewNamespace);

            _files.Remove(screen + "/Prefabs/GameplayScreen.prefab");
            plan = new ModuleRenamePlan(Lookups()).Build(picked, "Hud", _ => true);

            Assert.IsEmpty(plan.ScreenAddresses);
            Assert.IsTrue(plan.Kept.Any(line => line.Contains("GameplayScreen.prefab")));
        }
    }
}
