using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FlowIoC.Editor.CodeGenerator.Menus.Module;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// Create Command, Create Model and Create View all write their binding into the module's
    /// context, and the context is no longer always called {module}Context.cs - a module whose
    /// Root roots a System or a Service names it for that role.
    /// </summary>
    public class ModuleContextFileTests
    {
        private ModuleContextFile _contextFile;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _contextFile = new ModuleContextFile();
            _folder = Path.Combine(Path.GetTempPath(), "FlowIoCModuleContextFileTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        private void Write(string fileName) => File.WriteAllText(Path.Combine(_folder, fileName), string.Empty);

        [Test]
        public void The_plain_context_is_found_where_the_module_has_one()
        {
            Write("PlayerContext.cs");

            Assert.AreEqual(Path.Combine(_folder, "PlayerContext.cs"), _contextFile.Find(_folder, "Player"));
        }

        [Test]
        public void A_system_modules_context_is_found_under_the_name_the_role_gave_it()
        {
            Write("PlayerSystemContext.cs");

            Assert.AreEqual(Path.Combine(_folder, "PlayerSystemContext.cs"), _contextFile.Find(_folder, "Player"));
        }

        [Test]
        public void A_service_modules_context_is_found_under_the_name_the_role_gave_it()
        {
            Write("CounterServiceContext.cs");

            Assert.AreEqual(Path.Combine(_folder, "CounterServiceContext.cs"), _contextFile.Find(_folder, "Counter"));
        }

        /// <summary>
        /// A test module's context carries its kind in the middle of the name, and the module it
        /// sits under is never asked for a role, so the kind is what the lookup varies.
        /// </summary>
        [Test]
        public void A_kind_is_kept_between_the_module_name_and_the_word_context()
        {
            Write("PlayerTestContext.cs");

            Assert.AreEqual(Path.Combine(_folder, "PlayerTestContext.cs"), _contextFile.Find(_folder, "Player", "Test"));
        }

        /// <summary>
        /// Nothing on disk is the shape of a module created without a Context. The caller is
        /// handed the plain name so the file it fails to open is the one the warning names.
        /// </summary>
        [Test]
        public void A_module_without_a_context_is_handed_the_plain_name_and_a_warning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No context found"));

            Assert.AreEqual(Path.Combine(_folder, "PlayerContext.cs"), _contextFile.Find(_folder, "Player"));
        }

        /// <summary>
        /// A module already named for its role would otherwise be looked up twice under the same
        /// file name, and the warning would say so twice over.
        /// </summary>
        [Test]
        public void A_name_that_already_ends_in_a_role_is_not_offered_as_two_candidates()
        {
            List<string> candidates = _contextFile.Candidates("CounterService", string.Empty);

            Assert.AreEqual(2, candidates.Count);
            CollectionAssert.AreEqual(
                new[] {"CounterServiceContext.cs", "CounterServiceSystemContext.cs"}, candidates);
        }
    }
}