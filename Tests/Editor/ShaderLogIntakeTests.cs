using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The decisions the shader bridge makes, tested without an import to fire. The bridge itself
    /// is an AssetPostprocessor and a call into ShaderUtil, neither of which a test can stand up -
    /// which is why every decision it makes lives here instead.
    /// </summary>
    public class ShaderLogIntakeTests
    {
        private ShaderLogIntake _intake;

        [SetUp]
        public void SetUp()
        {
            _intake = new ShaderLogIntake();
        }

        [TestCase("Assets/Art/Water.shader")]
        [TestCase("Assets/Art/Water.shadergraph")]
        [TestCase("Assets/Art/Blur.compute")]
        [TestCase("Assets/Art/Trace.raytrace")]
        [TestCase("Assets/Art/WATER.SHADER")]
        public void An_asset_that_can_carry_shader_messages_is_recognised(string assetPath)
        {
            Assert.IsTrue(_intake.IsShaderAsset(assetPath));
        }

        /// <summary>
        /// An include holds no messages of its own. Editing one reimports every shader that
        /// includes it, and that is where the error actually surfaces, so listing .hlsl here would
        /// buy nothing and cost a load of every include on every import.
        /// </summary>
        [TestCase("Assets/Art/Common.hlsl")]
        [TestCase("Assets/Art/Common.cginc")]
        [TestCase("Assets/Scripts/Thing.cs")]
        [TestCase("")]
        [TestCase(null)]
        public void Anything_else_is_not(string assetPath)
        {
            Assert.IsFalse(_intake.IsShaderAsset(assetPath));
        }

        [Test]
        public void An_error_not_seen_before_is_reported()
        {
            Assert.IsTrue(_intake.ShouldReport("Assets/Water.shader", new List<string> {"undeclared identifier"}));
        }

        /// <summary>
        /// Unity reimports a shader whenever a file it includes is touched. Without this a project
        /// with one broken shader would put the same rows in the console every time anybody saved
        /// an .hlsl.
        /// </summary>
        [Test]
        public void The_same_errors_on_a_reimport_are_not_reported_twice()
        {
            var errors = new List<string> {"undeclared identifier"};

            _intake.ShouldReport("Assets/Water.shader", errors);

            Assert.IsFalse(_intake.ShouldReport("Assets/Water.shader", new List<string> {"undeclared identifier"}));
        }

        [Test]
        public void A_different_error_on_the_same_shader_is_reported()
        {
            _intake.ShouldReport("Assets/Water.shader", new List<string> {"undeclared identifier"});

            Assert.IsTrue(_intake.ShouldReport("Assets/Water.shader", new List<string> {"syntax error"}));
        }

        /// <summary>
        /// The one that was wrong first time round. The bridge used to return before asking when a
        /// shader had no messages, so a shader that was fixed stayed remembered as broken - and
        /// breaking it again with the error it had before was read as a repeat and never reported.
        /// Asking with an empty list is what forgets it.
        /// </summary>
        [Test]
        public void A_shader_that_compiles_is_forgotten_so_the_same_error_reports_again_later()
        {
            var errors = new List<string> {"undeclared identifier"};

            _intake.ShouldReport("Assets/Water.shader", errors);
            _intake.ShouldReport("Assets/Water.shader", new List<string>());

            Assert.IsTrue(_intake.ShouldReport("Assets/Water.shader", new List<string> {"undeclared identifier"}),
                "The error was swallowed as a repeat of a row no longer on screen.");
        }

        [Test]
        public void A_shader_that_compiles_reports_nothing()
        {
            Assert.IsFalse(_intake.ShouldReport("Assets/Water.shader", new List<string>()));
        }

        [Test]
        public void A_deleted_shader_is_forgotten()
        {
            var errors = new List<string> {"undeclared identifier"};

            _intake.ShouldReport("Assets/Water.shader", errors);
            _intake.Forget("Assets/Water.shader");

            Assert.IsTrue(_intake.ShouldReport("Assets/Water.shader", new List<string> {"undeclared identifier"}));
        }

        /// <summary>
        /// The row carries the compiler's words and nothing else. Unity's messageDetails is nine
        /// hundred characters of UNITY_* keywords, and a row carrying that is a row nobody can read.
        /// </summary>
        [Test]
        public void The_row_says_the_shader_the_error_and_the_platform()
        {
            string row = _intake.Describe("Hidden/Water", "undeclared identifier 'foam'", "D3D");

            Assert.AreEqual("Hidden/Water - undeclared identifier 'foam' (D3D)", row);
        }

        [Test]
        public void A_shader_with_no_name_still_gets_a_row()
        {
            Assert.AreEqual("syntax error (D3D)", _intake.Describe(null, "syntax error", "D3D"));
        }

        [Test]
        public void A_message_with_no_words_still_says_what_happened()
        {
            Assert.AreEqual("Hidden/Water - compilation failed (D3D)", _intake.Describe("Hidden/Water", "", "D3D"));
        }

        [Test]
        public void The_detail_is_empty_rather_than_a_blank_line_when_there_is_none()
        {
            Assert.IsNull(_intake.Detail(""));
            Assert.IsNull(_intake.Detail(null));
            Assert.AreEqual("Platform defines: ...", _intake.Detail("Platform defines: ..."));
        }

        /// <summary>
        /// Unity writes its own copy of the error to its console when the variant compiles, wrapped
        /// in a sentence of its own and carrying no file. Recognising it is what keeps the console
        /// from showing the same error twice, once clickable and once not.
        /// </summary>
        [Test]
        public void Unitys_own_copy_of_a_reported_error_is_recognised()
        {
            _intake.Remember("undeclared identifier 'foam'");

            Assert.IsTrue(_intake.WasReported(
                "Shader error in 'Hidden/Water': undeclared identifier 'foam' at Assets/Water.shader(11) (on d3d11)"));
        }

        /// <summary>
        /// A variant that only fails at play time was never imported, so nothing was recorded for
        /// it and Unity's copy is the only one there is. Dropping it would lose the error.
        /// </summary>
        [Test]
        public void An_error_that_was_never_reported_is_not_recognised()
        {
            _intake.Remember("undeclared identifier 'foam'");

            Assert.IsFalse(_intake.WasReported(
                "Shader error in 'Hidden/Water': syntax error at Assets/Water.shader(11) (on d3d11)"));
        }

        [Test]
        public void Nothing_is_recognised_before_anything_has_been_reported()
        {
            Assert.IsFalse(_intake.WasReported("Shader error in 'Hidden/Water': undeclared identifier 'foam'"));
        }
    }

    public class ShaderChannelTests
    {
        [Test]
        public void A_shader_log_goes_on_the_Shader_channel()
        {
            Assert.AreEqual(SystemLogType.Shader, FlowLogger.ChannelForSource(LogSource.Shader));
        }

        [Test]
        public void The_other_doors_are_unchanged()
        {
            Assert.AreEqual(SystemLogType.Compiler, FlowLogger.ChannelForSource(LogSource.Compiler));
            Assert.AreEqual(SystemLogType.Unity, FlowLogger.ChannelForSource(LogSource.Unity));
            Assert.AreEqual(SystemLogType.Unity, FlowLogger.ChannelForSource(LogSource.Flow));
        }

        /// <summary>
        /// Unity's wording for a shader diagnostic, which is what the Unity bridge matches to know
        /// it may be looking at a second copy of something already on the Shader channel.
        /// </summary>
        [TestCase("Shader error in 'Hidden/Water': undeclared identifier at Assets/Water.shader(11) (on d3d11)")]
        [TestCase("Shader warning in 'Hidden/Water': implicit truncation of vector type")]
        public void Unitys_wording_for_a_shader_diagnostic_is_recognised(string message)
        {
            Assert.IsTrue(new UnityLogIntake().IsShaderMessage(message));
        }

        [TestCase("Assets/Thing.cs(12,5): error CS0103: The name 'x' does not exist")]
        [TestCase("A shader error in another sentence entirely")]
        [TestCase("NullReferenceException: Object reference not set to an instance of an object")]
        [TestCase(null)]
        public void Anything_else_is_not_taken_for_one(string message)
        {
            Assert.IsFalse(new UnityLogIntake().IsShaderMessage(message));
        }
    }
}
