using System;
using System.Collections.Generic;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.Enums;

namespace Modules.MobileNotificationModule.Services
{
    /// <summary>
    /// The platform boundary. One implementation is bound per target in the Context; the
    /// commands never learn which. Every call is a plain forward to the OS - the decisions
    /// (which template, whether permission allows it) were taken in the command that called.
    /// </summary>
    internal interface INotificationGateway
    {
        /// <summary>Readies the platform and registers the channels. Called once, from Setup.</summary>
        void Initialize(IReadOnlyList<NotificationChannelCVO> channels);

        NotificationPermission ReadPermission();

        /// <summary>Asks the OS. Answers through the callback, once, on the main thread, possibly on the same frame.</summary>
        void RequestPermission(Action<NotificationPermission> answered);

        void Schedule(NotificationDraftVO draft);

        /// <summary>Takes back a scheduled one, and a delivered one still in the tray.</summary>
        void Cancel(NotificationIdentityVO identity);

        void CancelAllScheduled();

        /// <summary>Empties the tray and, where there is one, the badge.</summary>
        void ClearDelivered();

        /// <summary>The identifier written into the notification that opened the app, or null.</summary>
        string ReadOpenedFrom();

        void OpenSettings();

        /// <summary>
        /// Readies the pictures the catalogue names, each a file under StreamingAssets, so that a
        /// later Schedule finds them where the platform can read a file. Called once, from Setup;
        /// may finish later, and a Schedule before then goes out without its picture.
        /// </summary>
        void PreparePictures(IReadOnlyList<string> pictures);

        /// <summary>The device path of a readied picture, or false when it is not there (yet).</summary>
        bool TryResolvePicture(string picture, out string path);
    }
}
