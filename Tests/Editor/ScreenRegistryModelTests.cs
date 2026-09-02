using System.Collections.Generic;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.Model.Registry;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Screens and managers arrive from different Roots in an order the registry does not
    /// control: a screen context registers in its Setup, a ScreenManager when its mediator
    /// registers. The old model indexed the manager first and threw when the screen came before it.
    /// </summary>
    public class ScreenRegistryModelTests
    {
        private class SettingsScreenView : ScreenView
        {
        }

        private class ShopScreenView : ScreenView
        {
        }

        private ScreenRegistryModel _registry;
        private Context _owner;

        [SetUp]
        public void SetUp()
        {
            _registry = new ScreenRegistryModel();
            _owner = new Context();
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static ScreenEntry Entry<TView>(Context owner, int managerId = 0, ScreenTag tag = ScreenTag.Default, string address = "Screen")
            where TView : ScreenView
        {
            return new ScreenEntry
            {
                ViewType = typeof(TView),
                Owner = owner,
                Screen = new ScreenCVO {ManagerId = managerId, Tag = tag, Load = ScreenLoadCVO.Addressable(address)}
            };
        }

        [Test]
        public void A_screen_registered_before_its_manager_is_found_once_both_are_there()
        {
            Assert.IsTrue(_registry.RegisterScreen(Entry<SettingsScreenView>(_owner)));
            _registry.RegisterScreenManager(new ScreenManagerVO {ManagerID = 0});

            Assert.IsNotNull(_registry.GetEntry(0, typeof(SettingsScreenView)));
            Assert.IsNotNull(_registry.GetScreenManager(0));
        }

        [Test]
        public void A_manager_registered_before_its_screens_is_found_too()
        {
            _registry.RegisterScreenManager(new ScreenManagerVO {ManagerID = 0});
            _registry.RegisterScreen(Entry<SettingsScreenView>(_owner));

            Assert.AreSame(_owner, _registry.GetEntry(0, typeof(SettingsScreenView)).Owner);
        }

        [Test]
        public void An_unknown_screen_comes_back_null_rather_than_throwing()
        {
            // The misses are reported through FlowLogger, which reaches the console only with
            // ENABLE_LOG on. The flag has to be set inside the test: the runner resets it before
            // the test body runs, so a SetUp assignment does not survive.
            LogAssert.ignoreFailingMessages = true;

            Assert.IsNull(_registry.GetEntry(0, typeof(SettingsScreenView)));
            Assert.IsFalse(_registry.TryGetEntry(0, typeof(SettingsScreenView), out ScreenEntry _));
            Assert.IsNull(_registry.GetScreenManager(3));
        }

        [Test]
        public void A_screen_without_a_load_key_is_rejected()
        {
            ScreenEntry entry = Entry<SettingsScreenView>(_owner);
            entry.Screen.Load = default;
            LogAssert.ignoreFailingMessages = true;

            Assert.IsFalse(_registry.RegisterScreen(entry));
            Assert.IsFalse(_registry.TryGetEntry(0, typeof(SettingsScreenView), out ScreenEntry _));
        }

        [Test]
        public void Registering_the_same_screen_twice_keeps_the_later_entry()
        {
            _registry.RegisterScreen(Entry<SettingsScreenView>(_owner, address: "First"));
            _registry.RegisterScreen(Entry<SettingsScreenView>(_owner, address: "Second"));

            Assert.AreEqual("Second", _registry.GetEntry(0, typeof(SettingsScreenView)).Screen.Load.Key);
            Assert.AreEqual(1, _registry.GetAllEntries().Count);
        }

        [Test]
        public void Entries_are_grouped_by_manager_and_by_tag()
        {
            _registry.RegisterScreen(Entry<SettingsScreenView>(_owner, managerId: 0, tag: ScreenTag.GroupA));
            _registry.RegisterScreen(Entry<ShopScreenView>(_owner, managerId: 1, tag: ScreenTag.GroupA));

            Assert.AreEqual(1, _registry.GetManagerEntries(0).Count);
            Assert.AreEqual(1, _registry.GetManagerEntries(1).Count);
            Assert.AreEqual(2, _registry.GetTagEntries(ScreenTag.GroupA).Count);
            Assert.AreEqual(0, _registry.GetTagEntries(ScreenTag.GroupB).Count);
        }

        [Test]
        public void Removing_an_entry_takes_it_out_of_every_grouping()
        {
            ScreenEntry entry = Entry<SettingsScreenView>(_owner, tag: ScreenTag.GroupA);
            _registry.RegisterScreen(entry);

            _registry.RemoveEntry(entry);

            Assert.IsFalse(_registry.TryGetEntry(0, typeof(SettingsScreenView), out ScreenEntry _));
            Assert.AreEqual(0, _registry.GetAllEntries().Count);
            Assert.AreEqual(0, _registry.GetManagerEntries(0).Count);
            Assert.AreEqual(0, _registry.GetTagEntries(ScreenTag.GroupA).Count);
        }

        [Test]
        public void Loaded_screens_are_the_entries_that_hold_an_instance()
        {
            GameObject host = new GameObject("Settings");
            ScreenEntry loaded = Entry<SettingsScreenView>(_owner);
            loaded.Loaded = host.AddComponent<SettingsScreenView>();
            _registry.RegisterScreen(loaded);
            _registry.RegisterScreen(Entry<ShopScreenView>(_owner));

            List<IScreenBody> all = _registry.GetAllLoadedScreens();
            List<IScreenBody> atManager = _registry.GetAllScreensAtManager(0);

            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(1, atManager.Count);
            Assert.AreSame(loaded.Loaded, all[0]);

            Object.DestroyImmediate(host);
        }

        [Test]
        public void Copying_from_the_declaration_resets_the_per_open_flags()
        {
            ScreenCVO screen = new ScreenCVO
            {
                Layer = 2, Tag = ScreenTag.GroupC, HasShowAnimation = true, HasHideAnimation = true,
                Load = ScreenLoadCVO.Addressable("Settings")
            };
            ScreenVO data = new ScreenVO
            {
                ScreenType = typeof(SettingsScreenView), ForceOpenAtFullLayer = true, ForceOpenAtDuplication = true,
                AddToHistory = true, Parameters = new object[] {1}
            };

            _registry.CopyDataFromConfig(data, screen);

            Assert.AreEqual(2, data.LayerIndex);
            Assert.AreEqual(ScreenTag.GroupC, data.Tag);
            Assert.IsTrue(data.HasShowAnimation);
            Assert.IsTrue(data.HasHideAnimation);
            Assert.IsFalse(data.ForceOpenAtFullLayer);
            Assert.IsFalse(data.ForceOpenAtDuplication);
            Assert.IsFalse(data.AddToHistory);
            Assert.IsNull(data.Parameters);
        }

        [Test]
        public void Copying_by_screen_data_looks_the_declaration_up_by_manager_and_type()
        {
            _registry.RegisterScreen(Entry<SettingsScreenView>(_owner, managerId: 1));
            ScreenVO data = new ScreenVO {ScreenType = typeof(SettingsScreenView), ManagerId = 1, LayerIndex = 9};

            _registry.CopyDataFromConfig(data);

            Assert.AreEqual(0, data.LayerIndex);
        }

        [Test]
        public void The_same_screen_can_be_registered_at_two_managers()
        {
            Assert.IsTrue(_registry.RegisterScreen(Entry<SettingsScreenView>(_owner, managerId: 0)));
            Assert.IsTrue(_registry.RegisterScreen(Entry<SettingsScreenView>(_owner, managerId: 1)));

            Assert.AreEqual(0, _registry.GetEntry(0, typeof(SettingsScreenView)).Screen.ManagerId);
            Assert.AreEqual(1, _registry.GetEntry(1, typeof(SettingsScreenView)).Screen.ManagerId);
        }

        [Test]
        public void Removing_one_registration_leaves_the_other_manager_alone()
        {
            ScreenEntry atZero = Entry<SettingsScreenView>(_owner, managerId: 0);
            ScreenEntry atOne = Entry<SettingsScreenView>(_owner, managerId: 1);
            _registry.RegisterScreen(atZero);
            _registry.RegisterScreen(atOne);

            _registry.RemoveEntry(atZero);

            Assert.IsFalse(_registry.TryGetEntry(0, typeof(SettingsScreenView), out ScreenEntry _));
            Assert.IsTrue(_registry.TryGetEntry(1, typeof(SettingsScreenView), out ScreenEntry remaining));
            Assert.AreSame(atOne, remaining);
        }

        [Test]
        public void A_lookup_for_a_screen_that_is_not_registered_is_silent()
        {
            Assert.IsFalse(_registry.TryGetEntry(0, typeof(ShopScreenView), out ScreenEntry entry));
            Assert.IsNull(entry);
        }
    }
}