using System.Collections.Generic;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class ScriptAssetSearchPlanTests
    {
        private readonly ScriptAssetSearchPlan _plan = new ScriptAssetSearchPlan();

        [Test]
        public void A_captured_source_path_is_tried_first_and_carries_its_line()
        {
            List<ScriptSearchAttempt> attempts =
                _plan.Build("Modules.Player.AddCurrencyCommand", "Assets/Modules/PlayerModule/A.cs", 22);

            Assert.AreEqual(ScriptSearchKind.DirectPath, attempts[0].Kind);
            Assert.AreEqual("Assets/Modules/PlayerModule/A.cs", attempts[0].Value);
            Assert.AreEqual(22, attempts[0].LineNumber);
        }

        /// <summary>
        /// The case the blamed type exists for: the offending command is not on the stack at
        /// all, because it released asynchronously. There is no path, so the type is all there
        /// is to go on - and the file opens at its first line, which is honest.
        /// </summary>
        [Test]
        public void With_no_path_the_blamed_type_is_what_is_searched_for()
        {
            List<ScriptSearchAttempt> attempts = _plan.Build("Modules.Player.AddCurrencyCommand", null, 0);

            Assert.AreEqual(ScriptSearchKind.BlameTypeName, attempts[0].Kind);
            Assert.AreEqual("Modules.Player.AddCurrencyCommand", attempts[0].Value);
            Assert.AreEqual(0, attempts[0].LineNumber);
        }

        [Test]
        public void An_absolute_path_is_also_tried_as_a_project_relative_one()
        {
            List<ScriptSearchAttempt> attempts =
                _plan.Build(null, "D:/work/Game/Assets/Modules/PlayerModule/A.cs", 22);

            Assert.IsTrue(attempts.Exists(a =>
                a.Kind == ScriptSearchKind.RelativePath && a.Value == "Assets/Modules/PlayerModule/A.cs"));
        }

        [Test]
        public void A_path_under_Packages_is_made_relative_the_same_way()
        {
            List<ScriptSearchAttempt> attempts =
                _plan.Build(null, "D:/work/Game/Packages/FlowIoC/Runtime/A.cs", 9);

            Assert.IsTrue(attempts.Exists(a =>
                a.Kind == ScriptSearchKind.RelativePath && a.Value == "Packages/FlowIoC/Runtime/A.cs"));
        }

        [Test]
        public void A_bare_file_name_is_the_last_thing_tried()
        {
            List<ScriptSearchAttempt> attempts =
                _plan.Build("Modules.Player.AddCurrencyCommand", "Assets/Modules/PlayerModule/A.cs", 22);

            Assert.AreEqual(ScriptSearchKind.FileName, attempts[attempts.Count - 1].Kind);
            Assert.AreEqual("A", attempts[attempts.Count - 1].Value);
        }

        [Test]
        public void Nothing_to_go_on_is_an_empty_plan_rather_than_a_throw()
        {
            Assert.AreEqual(0, _plan.Build(null, null, 0).Count);
        }

        /// <summary>
        /// A Windows path arrives with backslashes and AssetDatabase only understands forward
        /// ones, so every attempt is normalised before it leaves here.
        /// </summary>
        [Test]
        public void A_windows_path_is_normalised()
        {
            List<ScriptSearchAttempt> attempts =
                _plan.Build(null, @"D:\work\Game\Assets\Modules\A.cs", 4);

            Assert.AreEqual("D:/work/Game/Assets/Modules/A.cs", attempts[0].Value);
            Assert.IsTrue(attempts.Exists(a =>
                a.Kind == ScriptSearchKind.RelativePath && a.Value == "Assets/Modules/A.cs"));
        }
    }
}
