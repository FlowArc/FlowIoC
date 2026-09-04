using FlowIoC.Editor.CodeGenerator.Menus.Module;
using FlowIoC.Editor.Config.ModuleConfig;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class OptionalFolderToggleRuleTests
    {
        private static FolderEVO Optional() =>
            new FolderEVO {Type = FolderEVO.FolderType.Signals, IsOptional = true, IsMandatory = false};

        private static FolderEVO Mandatory() =>
            new FolderEVO {Type = FolderEVO.FolderType.Signals, IsOptional = false, IsMandatory = true};

        /// <summary>
        /// The screen and test layouts carry no Shared folder at all, so the toggle for it has
        /// nothing to tick and is not drawn - no module type has to be named for that to happen.
        /// </summary>
        [Test]
        public void A_folder_the_layout_does_not_have_is_not_offered()
        {
            OptionalFolderToggleState state =
                new OptionalFolderToggleRule().For(null, ModuleType.Main, new ModuleType[0]);

            Assert.AreEqual(OptionalFolderToggleState.Hidden, state);
        }

        [Test]
        public void An_optional_folder_is_the_readers_to_choose()
        {
            OptionalFolderToggleState state =
                new OptionalFolderToggleRule().For(Optional(), ModuleType.Main, new ModuleType[0]);

            Assert.AreEqual(OptionalFolderToggleState.Selectable, state);
        }

        /// <summary>
        /// The screen layout marks Signals mandatory, because a screen module generates no Context
        /// and its holder is the only way in. The toggle then says so rather than disappearing.
        /// </summary>
        [Test]
        public void A_mandatory_folder_is_shown_on_and_cannot_be_turned_off()
        {
            OptionalFolderToggleState state =
                new OptionalFolderToggleRule().For(Mandatory(), ModuleType.Screen, new ModuleType[0]);

            Assert.AreEqual(OptionalFolderToggleState.ForcedOn, state);
        }

        /// <summary>
        /// A test module wires other modules' signals rather than owning a public surface of its
        /// own, so it is withheld the Signals toggle even though its layout offers the folder.
        /// </summary>
        [Test]
        public void A_module_type_the_folder_is_withheld_from_is_not_offered_it()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Optional(), ModuleType.Test, new[] {ModuleType.Test});

            Assert.AreEqual(OptionalFolderToggleState.Hidden, state);
        }

        [Test]
        public void Withholding_one_module_type_leaves_the_others_alone()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Optional(), ModuleType.Main, new[] {ModuleType.Test});

            Assert.AreEqual(OptionalFolderToggleState.Selectable, state);
        }

        /// <summary>
        /// A screen module's signal holder lives in Shared and is the only way into the screen, so
        /// the folder is given to it ticked and locked even though the layout calls it optional.
        /// </summary>
        [Test]
        public void A_module_type_the_folder_is_required_for_is_given_it_locked_on()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Optional(), ModuleType.Screen, new ModuleType[0], new[] {ModuleType.Screen});

            Assert.AreEqual(OptionalFolderToggleState.ForcedOn, state);
        }

        [Test]
        public void Requiring_one_module_type_leaves_the_others_choosing()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Optional(), ModuleType.Main, new ModuleType[0], new[] {ModuleType.Screen});

            Assert.AreEqual(OptionalFolderToggleState.Selectable, state);
        }

        /// <summary>
        /// Withholding wins over requiring as it wins over mandatory: whether a module type is
        /// offered the folder at all is settled before what the folder says about itself.
        /// </summary>
        [Test]
        public void A_withheld_module_type_is_not_given_a_required_folder()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Optional(), ModuleType.Test, new[] {ModuleType.Test}, new[] {ModuleType.Test});

            Assert.AreEqual(OptionalFolderToggleState.Hidden, state);
        }

        /// <summary>
        /// Withholding wins over mandatory. A layout that marks the folder mandatory is describing
        /// the folder, not deciding which module types are offered a toggle for it.
        /// </summary>
        [Test]
        public void A_withheld_module_type_is_not_offered_a_mandatory_folder_either()
        {
            OptionalFolderToggleState state = new OptionalFolderToggleRule()
                .For(Mandatory(), ModuleType.Test, new[] {ModuleType.Test});

            Assert.AreEqual(OptionalFolderToggleState.Hidden, state);
        }
    }
}