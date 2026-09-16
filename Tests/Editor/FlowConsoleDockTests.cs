using System;
using System.Linq;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEditor;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The console docks itself beside Unity's Console the first time a project meets FlowIoC,
    /// and Unity's Console is an internal type reached by name. This is the test that notices a
    /// Unity upgrade renaming it - the dock would otherwise quietly fall back to a floating
    /// window and nobody would know why.
    /// </summary>
    public class FlowConsoleDockTests
    {
        [Test]
        public void The_console_docks_beside_Unitys_Console_and_then_the_Project_window()
        {
            Type[] neighbours = FlowConsoleEditor.DockNeighbours();

            Assert.AreEqual(2, neighbours.Length, "UnityEditor.ConsoleWindow or UnityEditor.ProjectBrowser no longer resolves by name");
            Assert.AreEqual("ConsoleWindow", neighbours[0].Name);
            Assert.AreEqual("ProjectBrowser", neighbours[1].Name);
            Assert.IsTrue(neighbours.All(type => typeof(EditorWindow).IsAssignableFrom(type)));
        }
    }
}
