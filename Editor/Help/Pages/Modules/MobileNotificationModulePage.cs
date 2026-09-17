#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Icons;

namespace FlowIoC.Editor.Help.Pages.Modules
{
    /// <summary>
    /// The mobile notification module: the catalogue, the permission step, the return reminders,
    /// and the three package settings that fail silently when skipped.
    /// </summary>
    internal class MobileNotificationModulePage : ModulePage
    {
        public override string ModuleFolderName => "MobileNotificationModule";

        public override string Title => "Mobile Notification Module";

        public override string Subtitle => "Local notifications from a catalogue, the permission ask as a step, come-back reminders for free";

        public override FlowIcon Icon => FlowIcon.Stopwatch;

        public override IReadOnlyList<string> RequiredPackages => new[] {"com.unity.mobile.notifications"};

        public override IReadOnlyList<HelpTab> MoreTabs => new[]
        {
            new HelpTab("Setup", DrawSetup,
                "The Root in the boot scene with the catalogue on its adapter, and three package settings.",
                "Each of the three settings fails silently when skipped: an icon name the package "
                + "does not know shows the app icon, a reboot forgets every scheduled notification, "
                + "and the package's own launch-time ask pre-empts the step the game binds."),
            new HelpTab("Usage", DrawUsage,
                "Bind the permission step where the ask belongs; schedule by template key from a Command.",
                "A computed time is a Schedule call in the Command that learned it. A fixed one is "
                + "a bound step. The return reminders need nothing."),
            new HelpTab("Catalogue", DrawCatalogue,
                "Channels the player can mute, templates scheduled by key, reminders that go out by themselves.",
                "One asset, CD_MobileNotifications, read once off the Root's adapter.")
        };

        public override string InstalledHint =>
            "Drop MobileNotificationServiceRoot into your boot scene and bind "
            + "IMobileNotificationService.Commands.RequestPermission where the ask belongs; the Setup tab has the steps.";

        public override string BodyHeadline =>
            "A template key and a time schedule a notification on the phone.";

        public override string BodyTagline =>
            "The chest that finishes in three hours, the event that starts tomorrow, the player who "
            + "has not opened the game in a week - each is one Schedule call, or nothing at all.";

        public override void DrawBody(HelpPainter painter)
        {
            painter.SubHeading("What it gives you");
            painter.Bullet(
                "Local notifications on iOS and Android through Unity's Mobile Notifications package, "
                + "with the texts, channels and icons in one catalogue asset rather than in code.");
            painter.Bullet(
                "The permission ask as a step - IMobileNotificationService.Commands.RequestPermission - "
                + "that holds the sequence until the OS answers, bound wherever the game decides to ask.");
            painter.Bullet(
                "Come-back reminders that go out by themselves when the app is left and are taken back "
                + "when it returns, from a list in the catalogue: one day, three days, a week.");
            painter.Bullet(
                "Which notification the player tapped to open the app, on the interface, for a boot "
                + "Command that wants to open the chest screen rather than the title.");
            painter.Bullet(
                "The Editor never shows a notification; its gateway logs every call on the module's "
                + "channel and logs an arrival when a scheduled time passes, so the test scene reads.");

            painter.Space();
            painter.Note(
                "It is a Service, so another module references Modules.MobileNotification and injects "
                + "IMobileNotificationService directly. It has no signals: what happened at boot is read "
                + "from the interface, and what a game schedules is the game's own decision.");
        }

        private void DrawSetup(HelpPainter painter)
        {
            painter.SubHeading("The Root");
            painter.Bullet("MobileNotificationServiceRoot into the scene the game boots from, Initialize Order -20.");
            painter.Bullet(
                "CD_MobileNotifications on the Root's adapter, in the module's own Scriptables slot. The "
                + "shipped asset has a Reminders channel, a Return template, a Test template and the "
                + "three reminders; edit it or file your own.");

            painter.Space();
            painter.SubHeading("Project Settings > Mobile Notifications");
            painter.Note(
                "Android > Notification Icons: register every SmallIcon and LargeIcon name the catalogue "
                + "uses. An unknown name falls back to the app icon with no error.");
            painter.Note(
                "Android > Reschedule on Device Restart: on. Off, a reboot forgets every scheduled "
                + "notification, the come-back reminders included, and nothing says so.");
            painter.Note(
                "iOS > Request Authorization on App Launch: off. The ask is the module's step, bound "
                + "where the game wants it; the package's own ask would pre-empt it at the first frame.");

            painter.Space();
            painter.SubHeading("The panel");
            painter.Paragraph(
                "Tools > FlowIoC-Modules > Mobile Notification > Panel edits the catalogue, previews each "
                + "template as the Android and iOS trays draw it - sample arguments filling its "
                + "placeholders - flags the three settings above when they are wrong, and, while the game "
                + "runs, lists what the service has scheduled with the card each one will show.");
        }

        private void DrawUsage(HelpPainter painter)
        {
            painter.SubHeading("The permission");
            painter.Code(
                "CommandBinder.Bind(_signals.Incoming.TutorialFinished)\n"
                + "    .ToSequence<IMobileNotificationService.Commands.RequestPermission>()\n"
                + "    .ToSequence<OpenMainScreenCommand>();",
                "The step holds the sequence until the OS dialog answers.");

            painter.Space();
            painter.SubHeading("A computed time");
            painter.Code(
                "[Inject] private IMobileNotificationService _notifications { get; set; }\n\n"
                + "_notifications.Schedule(\"ChestReady\", chest.Remaining, slot.ToString(), chest.Name);",
                "Key, delay, an instance tag, and the args that fill {0} in the template's texts.");

            painter.Space();
            painter.SubHeading("A fixed time, cancel, everything");
            painter.Code(
                ".ToSequence<IMobileNotificationService.Commands.Schedule>(\"FreeChest\")\n"
                + ".ToSequence<IMobileNotificationService.Commands.Cancel>(\"FreeChest\")\n"
                + ".ToSequence<IMobileNotificationService.Commands.CancelAll>()",
                "Schedule takes the template's DefaultAfterMinutes.");

            painter.Space();
            painter.SubHeading("What the player tapped");
            painter.Code(
                "if (_notifications.OpenedFromKey == \"ChestReady\")\n"
                + "    _screenSignals.Incoming.OpenChests.Dispatch();",
                "In a boot Command, once the screens are up.");
        }

        private void DrawCatalogue(HelpPainter painter)
        {
            painter.SubHeading("Channels");
            painter.Paragraph(
                "Key, Name, Description, Importance. Android registers each as a channel the player can "
                + "mute in the OS settings; iOS groups notifications by the key and ignores the rest.");

            painter.Space();
            painter.SubHeading("Notifications");
            painter.Paragraph(
                "Key, Channel, Title, Body, SmallIcon, LargeIcon, Picture, ShowInForeground, Repeats, "
                + "DefaultAfterMinutes. Title and Body may carry {0}, {1} placeholders. Repeats means at "
                + "the interval it was scheduled with - scheduled at a time, daily at that time.");

            painter.Space();
            painter.SubHeading("Picture");
            painter.Paragraph(
                "An image under Assets/StreamingAssets, named by its relative path - Notifications/chest.png. "
                + "Android shows it under the text when the notification is expanded, iOS as the thumbnail "
                + "at the right of the banner, and the panel's preview draws both. At initialize the module "
                + "copies every picture the catalogue names to the device once, because both trays read a "
                + "file path; a notification scheduled before its copy is there goes out without it, and "
                + "says so in the log.");

            painter.Space();
            painter.SubHeading("Return reminders");
            painter.Paragraph(
                "A template key and AfterMinutes. Every entry is scheduled when the app is left and "
                + "cancelled when it returns, each under its own tag - Return#1440, Return#4320, "
                + "Return#10080. Minutes everywhere: a day is 1440, a device test 0.2.");
        }
    }
}

#endif