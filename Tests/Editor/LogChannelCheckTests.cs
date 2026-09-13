using System;
using System.Collections.Generic;
using FlowIoC.Editor.ModuleScanner;
using FlowIoC.Editor.Modules;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class LogChannelCheckTests
    {
        private const string Root = "C:/proj/Assets/Modules/PlayerModule";

        private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);

        private static ModuleTargetEVO Target(ModuleKind kind = ModuleKind.Main, string name = "PlayerModule", string parent = null) =>
            new ModuleTargetEVO
            {
                Name = name,
                Kind = kind,
                ParentName = parent,
                AbsolutePath = Root,
                AssetPath = "Assets/Modules/PlayerModule"
            };

        private LogChannelCheck Check() =>
            new LogChannelCheck(
                module => new List<string>(_files.Keys),
                path => _files[path],
                (path, rewrite) => _files[path] = rewrite(_files[path]),
                name => name == "PlayerModule" || name == "ShopModule" || name == "Default");

        private string File(string content, string name = "AddCurrencyCommand.cs")
        {
            string path = Root + "/Scripts/Runtime/Controllers/" + name;
            _files[path] = content;
            return path;
        }

        [SetUp]
        public void Clear() => _files.Clear();

        [Test]
        public void A_module_that_names_no_channel_is_Ok()
        {
            File("FlowLogger.Log(\"Execute - AddCurrencyCommand\");\nFlowLogger.LogError($\"Save failed: {error}\", _config);");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Target()).Status);
        }

        [Test]
        public void A_channel_typed_as_a_string_is_reported_with_its_file_and_line()
        {
            string path = File("// header\nFlowLogger.Log(\"Player\", \"Granted 100 soft currency.\");");

            FindingEVO finding = Check().Inspect(Target());

            Assert.AreEqual(ModuleCheckStatus.Fixable, finding.Status);
            StringAssert.Contains("AddCurrencyCommand.cs:2", finding.Message);
            Assert.AreEqual("Assets/Modules/PlayerModule/Scripts/Runtime/Controllers/AddCurrencyCommand.cs", finding.AssetPath);
            Assert.IsTrue(_files.ContainsKey(path));
        }

        /// <summary>
        /// Where the message is all that follows, the channel goes and the call is the channel-less
        /// overload; the message keeps its own text, interpolation and all.
        /// </summary>
        [Test]
        public void Fix_cuts_a_typed_channel_when_only_the_message_follows()
        {
            string path = File("FlowLogger.Log(\"Player\",\n    $\"Purchase '{_itemId}' for {_price}.\");");

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.Log($\"Purchase '{_itemId}' for {_price}.\");", _files[path]);
        }

        /// <summary>
        /// No channel-less overload takes a profile, so a call that passes more than the message
        /// keeps a channel - the module's own constant, read off the file the way the logger would.
        /// </summary>
        [Test]
        public void Fix_writes_the_modules_constant_when_more_than_the_message_follows()
        {
            string path = File("FlowLogger.Log(\"Player\", \"Currency went negative.\", Warning);");

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.Log(FlowModule.PlayerModule, \"Currency went negative.\", Warning);", _files[path]);
        }

        /// <summary>
        /// A literal that is exactly another module's name was about that module; only the
        /// spelling was wrong, so it becomes that module's constant rather than being cut.
        /// </summary>
        [Test]
        public void Fix_turns_another_modules_name_into_its_constant()
        {
            string path = File("FlowLogger.LogWarning(\"ShopModule\", \"Player cannot afford this item.\");");

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.LogWarning(FlowModule.ShopModule, \"Player cannot afford this item.\");", _files[path]);
        }

        [Test]
        public void The_modules_own_constant_is_redundant_and_is_cut()
        {
            string path = File("FlowLogger.LogError(FlowModule.PlayerModule, \"Currency went negative.\");");

            Assert.AreEqual(ModuleCheckStatus.Fixable, Check().Inspect(Target()).Status);

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.LogError(\"Currency went negative.\");", _files[path]);
        }

        /// <summary>
        /// A profile names its channel by design, and a context object cannot be told from one by
        /// reading the text - so the module's own constant with more than the message after it is
        /// left as written.
        /// </summary>
        [Test]
        public void The_modules_own_constant_stays_when_a_profile_or_a_context_follows()
        {
            File("FlowLogger.Log(FlowModule.PlayerModule, message, Economy);\nFlowLogger.LogError(FlowModule.PlayerModule, \"x\", _config);");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Target()).Status);
        }

        [Test]
        public void Another_modules_constant_is_the_Connectors_case_and_is_Ok()
        {
            File("FlowLogger.Log(FlowModule.ShopModule, \"Wired the shop to the player.\");");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Target()).Status);
        }

        [Test]
        public void Default_inside_a_module_is_a_channel_named_by_hand()
        {
            string path = File("FlowLogger.Log(FlowModule.Default, \"a\");\nFlowLogger.Log(FlowModule.Default, \"b\", Economy);");

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.Log(\"a\");\nFlowLogger.Log(FlowModule.PlayerModule, \"b\", Economy);", _files[path]);
        }

        /// <summary>A test module's lines land on the module it tests, so that is the constant that is redundant there.</summary>
        [Test]
        public void A_test_module_logs_on_its_parent()
        {
            string path = File("FlowLogger.Log(FlowModule.PlayerModule, \"probe\");");
            ModuleTargetEVO test = Target(ModuleKind.Test, "PlayerTestModule", "PlayerModule");

            Assert.AreEqual(ModuleCheckStatus.Fixable, Check().Inspect(test).Status);

            Check().Fix(test);

            Assert.AreEqual("FlowLogger.Log(\"probe\");", _files[path]);
        }

        [Test]
        public void A_variable_or_a_channel_less_call_is_not_reported()
        {
            File("FlowLogger.Log(channel, \"a\");\nFlowLogger.Log(\"only the message\");\nFlowLogger.Log(\"a\" + b);");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Target()).Status);
        }

        /// <summary>
        /// A literal message and then a context object is the channel-less LogError. A bare word
        /// followed by a value that is no string could be either shape, so it is left alone; the
        /// same bare word followed by a string is a channel for certain.
        /// </summary>
        [Test]
        public void An_error_with_a_literal_message_and_a_context_is_not_a_typed_channel()
        {
            File(
                "FlowLogger.LogError(\"Save failed.\", _config);\nFlowLogger.LogError($\"Icon missing for '{_weaponId}'.\", this);\nFlowLogger.LogError(\"Nope\", context);");

            Assert.AreEqual(ModuleCheckStatus.Ok, Check().Inspect(Target()).Status);
        }

        /// <summary>
        /// Log has no context overload, so a bare word and then a variable can only be a channel
        /// and a message; and a third argument after a literal puts any method in the channel-first
        /// shape.
        /// </summary>
        [Test]
        public void A_typed_channel_with_a_variable_message_is_still_reported_where_the_shape_is_certain()
        {
            string path = File("FlowLogger.Log(\"Player\", message);\nFlowLogger.LogError(\"Player\", \"x\", _config);");

            Assert.AreEqual(ModuleCheckStatus.Fixable, Check().Inspect(Target()).Status);

            Check().Fix(Target());

            Assert.AreEqual("FlowLogger.Log(message);\nFlowLogger.LogError(FlowModule.PlayerModule, \"x\", _config);", _files[path]);
        }

        [Test]
        public void Several_lines_are_listed_and_the_rest_counted()
        {
            var lines = new List<string>();
            for (int index = 0; index < 8; index++)
                lines.Add("FlowLogger.Log(\"Player\", \"" + index + "\");");

            File(string.Join("\n", lines));

            FindingEVO finding = Check().Inspect(Target());

            StringAssert.Contains("8 lines name a channel by hand", finding.Message);
            StringAssert.Contains("AddCurrencyCommand.cs:6", finding.Message);
            StringAssert.Contains("and 2 more", finding.Message);
            StringAssert.DoesNotContain("AddCurrencyCommand.cs:7", finding.Message);
        }

        [Test]
        public void A_file_under_a_nested_module_belongs_to_that_module()
        {
            Assert.IsTrue(LogChannelCheck.IsOwnFile(Root, Root + "/Scripts/Runtime/Models/PlayerModel.cs"));
            Assert.IsTrue(LogChannelCheck.IsOwnFile(Root, Root + "\\Scripts\\Editor\\PlayerModuleEditor.cs"));
            Assert.IsFalse(LogChannelCheck.IsOwnFile(Root,
                Root + "/zScreenModules/MainScreenModule/Scripts/Runtime/Controllers/OpenMainScreenCommand.cs"));
            Assert.IsFalse(LogChannelCheck.IsOwnFile(Root, Root + "/zTestModules/PlayerTestModule/Scripts/Runtime/PlayerTestContext.cs"));
        }
    }
}