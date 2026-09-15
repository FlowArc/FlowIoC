using System;
using System.Collections.Generic;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The module's one counterpart. Injecting this interface is the sanctioned cross-module
    /// reference: a game's Command schedules a notification at the point where it learned the
    /// time - the chest's remaining duration, the event's start - and the template it names lives
    /// in CD_MobileNotifications. The steps a game binds instead of writing that Command sit
    /// under <see cref="Commands"/>, one file each beside this one. The module never asks for
    /// permission by itself; <c>Commands.RequestPermission</c> is bound where the game wants it.
    /// </summary>
    public partial interface IMobileNotificationService
    {
        /// <summary>What the OS last said. NotAsked until a request was answered, or read at initialize.</summary>
        NotificationPermission Permission { get; }

        /// <summary>The template key of the notification the player tapped to open the app; empty when none did.</summary>
        string OpenedFromKey { get; }

        /// <summary>The tag of that notification; empty when it had none.</summary>
        string OpenedFromTag { get; }

        /// <summary>What this session scheduled, as the module remembers it; the OS is not asked.</summary>
        IReadOnlyList<NotificationDraftVO> Scheduled { get; }

        /// <summary>Asks the OS once. The callback fires once, on the main thread, possibly on the same frame.</summary>
        void RequestPermission(Action<NotificationPermission> answered = null);

        /// <summary>Schedules the template after its own DefaultAfterMinutes; a template with none is reported.</summary>
        void Schedule(string key, string tag = null, params object[] args);

        /// <summary>Schedules the template after a delay. The same key and tag scheduled again replaces the earlier one.</summary>
        void Schedule(string key, TimeSpan after, string tag = null, params object[] args);

        /// <summary>Schedules the template at a local time; a repeating template then repeats daily at that time.</summary>
        void Schedule(string key, DateTime at, string tag = null, params object[] args);

        /// <summary>Takes back one scheduled notification, and its delivered copy if it is still in the tray.</summary>
        void Cancel(string key, string tag = null);

        /// <summary>Takes back everything scheduled, the return reminders included.</summary>
        void CancelAll();

        /// <summary>Opens the OS notification settings for the app - for a settings screen's "notifications are off" state.</summary>
        void OpenSettings();

        /// <summary>
        /// The steps a game binds in a sequence of its own. They sit inside the interface so that
        /// the one name a game knows - the Service it injects - is also where its steps are found,
        /// and the flow reads from the Context. Each step is a file of its own,
        /// <c>IMobileNotificationService.Commands.&lt;Step&gt;.cs</c>.
        /// </summary>
        public static partial class Commands
        {
        }
    }
}
