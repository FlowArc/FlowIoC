using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FlowIoC.Editor.AgentSkills;
using FlowIoC.Editor.Help.Pages;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The page is the readable half of a convention whose other half is serialised into the
    /// prefabs the package ships. A number changed in one and not the other leaves a reader
    /// with a table the scene contradicts, so the two are checked against each other here.
    /// </summary>
    public class OrderingRootsPageTests
    {
        private OrderingRootsPage _page;

        /// <summary>Where each Root's initializeOrder actually lives, relative to the package root.</summary>
        private readonly Dictionary<string, string> _prefabs = new Dictionary<string, string>
        {
            {"ScreenServiceRoot", "Assets/Prefabs/ScreenServiceRoot.prefab"},
            {"PoolServiceRoot", "Assets/Prefabs/PoolServiceRoot.prefab"},
            {"AssetServiceRoot", "Assets/Prefabs/AssetServiceRoot.prefab"},
            {"GameplayRoot", "SetupModules~/GameplayModule/Prefabs/GameplayRoot.prefab"},
            {"ScreenRoot", "SetupModules~/ScreenModule/Prefabs/ScreenRoot.prefab"},
            {"MainRoot", "SetupModules~/MainModule/Prefabs/MainRoot.prefab"},
            {"ConnectorRoot", "SetupModules~/ConnectorModule/Prefabs/ConnectorRoot.prefab"}
        };

        [SetUp]
        public void SetUp() => _page = new OrderingRootsPage();

        [Test]
        public void Every_seat_the_page_shows_matches_the_prefab_that_ships()
        {
            foreach (KeyValuePair<string, int> seat in _page.Seats)
            {
                Assert.IsTrue(_prefabs.ContainsKey(seat.Key),
                    $"The page names '{seat.Key}' but the test knows no prefab to check it against.");

                Assert.AreEqual(seat.Value, OrderIn(_prefabs[seat.Key]),
                    $"'{seat.Key}' is drawn at {seat.Value} but its prefab says otherwise.");
            }
        }

        /// <summary>
        /// The three numbers at the top are the whole point of the convention: the wiring comes
        /// after the modules it wires, the screen host is up before the flow that opens a screen,
        /// and the entry point launches after everything else.
        /// </summary>
        [Test]
        public void The_connector_the_screen_host_and_the_entry_point_keep_their_seats()
        {
            Assert.AreEqual(98, _page.Seats["ConnectorRoot"]);
            Assert.AreEqual(99, _page.Seats["ScreenRoot"]);
            Assert.AreEqual(100, _page.Seats["MainRoot"]);
        }

        /// <summary>
        /// A Service depends on nothing, so it has no reason to wait for a module and every reason
        /// to be ready before one asks for it.
        /// </summary>
        [Test]
        public void The_services_come_up_before_the_game_s_own_modules()
        {
            var services = new[] {"ScreenServiceRoot", "PoolServiceRoot", "AssetServiceRoot"};

            int firstModule = _page.Seats
                .Where(seat => !services.Contains(seat.Key))
                .Min(seat => seat.Value);

            foreach (string service in services)
                Assert.Less(_page.Seats[service], firstModule);
        }

        [Test]
        public void The_page_ships_the_hierarchy_it_shows()
        {
            string image = Path.Combine(PackageRoot(), "Editor/Help/Images/MainSceneHierarchy.png");

            Assert.IsTrue(File.Exists(image), $"The screenshot the page draws is missing: '{image}'.");
        }

        [Test]
        public void The_page_offers_a_second_reading()
        {
            CollectionAssert.AreEqual(new[] {"Introduction", "Picking a number"},
                _page.Tabs.Select(tab => tab.Title).ToList());
        }

        private int OrderIn(string prefabPath)
        {
            string full = Path.Combine(PackageRoot(), prefabPath);

            Assert.IsTrue(File.Exists(full), $"The package no longer ships '{prefabPath}'.");

            Match order = Regex.Match(File.ReadAllText(full), @"initializeOrder:\s*(-?\d+)");

            Assert.IsTrue(order.Success, $"'{prefabPath}' serialises no initializeOrder.");

            return int.Parse(order.Groups[1].Value);
        }

        /// <summary>
        /// The package resolves to a hashed folder under Library/PackageCache for a UPM install,
        /// so the path is asked of the source that already knows how to find it rather than
        /// assumed: Documentation~/Skills sits two folders below the package root.
        /// </summary>
        private string PackageRoot() => Directory.GetParent(new AgentSkillsSource().Root).Parent.FullName;
    }
}