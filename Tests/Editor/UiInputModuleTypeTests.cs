using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;
using UnityEngine.EventSystems;

namespace FlowIoC.Tests
{
    /// <summary>
    /// A scene FlowIoC authors gets an EventSystem, and the input module beside it decides whether
    /// that scene works at all: the legacy one throws in a project whose active input handling is
    /// the Input System alone, which is what a new Unity 6 project is set to.
    /// </summary>
    public class UiInputModuleTypeTests
    {
        [Test]
        public void The_type_the_lookup_finds_is_the_one_used()
        {
            var resolver = new UiInputModuleType(name => typeof(UiInputModuleTypeTests));

            Assert.AreEqual(typeof(UiInputModuleTypeTests), resolver.Resolve());
        }

        [Test]
        public void The_legacy_module_is_the_fallback_when_the_lookup_finds_nothing()
        {
            var resolver = new UiInputModuleType(name => null);

            Assert.AreEqual(typeof(StandaloneInputModule), resolver.Resolve());
        }

        [Test]
        public void The_lookup_is_asked_for_the_Input_System_module_by_assembly_qualified_name()
        {
            string asked = null;

            new UiInputModuleType(name =>
            {
                asked = name;
                return null;
            }).Resolve();

            Assert.AreEqual(UiInputModuleType.InputSystemTypeName, asked);
        }

        /// <summary>
        /// The name is a string, so nothing but a real lookup catches a typo in it - and a typo
        /// would not fail, it would quietly fall back to the legacy module forever. This project
        /// has com.unity.inputsystem, so the real lookup has to find it.
        /// </summary>
        [Test]
        public void The_real_lookup_finds_the_Input_System_module_in_this_project()
        {
            Assert.AreEqual(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule",
                new UiInputModuleType().Resolve().FullName);
        }
    }
}
