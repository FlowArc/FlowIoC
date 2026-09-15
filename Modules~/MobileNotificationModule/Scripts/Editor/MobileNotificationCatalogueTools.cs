#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Modules.MobileNotificationModule.Data.UnityObjects;
using Modules.MobileNotificationModule.Data.ValueObjects;
using UnityEditor;

namespace Modules.MobileNotificationModule.Editor
{
    /// <summary>
    /// What the panel knows about a catalogue that the asset does not say by itself: which entries
    /// cannot work and why, how a reminder's minutes read in words, and what a template's texts
    /// become once sample arguments fill their placeholders. It reads the asset through
    /// SerializedObject so the panel's edits go through Undo and dirty the asset the way the
    /// inspector's would.
    /// </summary>
    internal class MobileNotificationCatalogueTools
    {
        public const string CHANNELS = "_channels";
        public const string NOTIFICATIONS = "_notifications";
        public const string RETURN_REMINDERS = "_returnReminders";

        public const string KEY = "Key";
        public const string NAME = "Name";
        public const string DESCRIPTION = "Description";
        public const string IMPORTANCE = "Importance";
        public const string CHANNEL = "Channel";
        public const string TITLE = "Title";
        public const string BODY = "Body";
        public const string SMALL_ICON = "SmallIcon";
        public const string LARGE_ICON = "LargeIcon";
        public const string PICTURE = "Picture";
        public const string SHOW_IN_FOREGROUND = "ShowInForeground";
        public const string REPEATS = "Repeats";
        public const string DEFAULT_AFTER_MINUTES = "DefaultAfterMinutes";
        public const string NOTIFICATION = "Notification";
        public const string AFTER_MINUTES = "AfterMinutes";

        private readonly Dictionary<CD_MobileNotifications, SerializedObject> _serialized = new();

        /// <summary>Every catalogue in the project, by path, so the panel lists the same ones the Project window would.</summary>
        public List<CD_MobileNotifications> Assets()
        {
            var assets = new List<CD_MobileNotifications>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CD_MobileNotifications)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<CD_MobileNotifications>(AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            return assets;
        }

        public SerializedObject Serialized(CD_MobileNotifications asset)
        {
            if (!_serialized.TryGetValue(asset, out SerializedObject serialized) || serialized.targetObject == null)
            {
                serialized = new SerializedObject(asset);
                _serialized[asset] = serialized;
            }

            return serialized;
        }

        /// <summary>
        /// Everything wrong with the catalogue, in the order the asset lists things, plus the icon
        /// names the project has not registered - the one fault the phone hides by drawing the app
        /// icon instead. <paramref name="registeredIcons"/> is what Project Settings > Mobile
        /// Notifications knows; null skips that check (a project with no Android target).
        /// <paramref name="pictureExists"/> answers whether a picture file is under StreamingAssets;
        /// null skips that check too.
        /// </summary>
        public List<CatalogueMessageEVO> Messages(CD_MobileNotifications asset, IReadOnlyCollection<string> registeredIcons,
            Func<string, bool> pictureExists = null)
        {
            var messages = new List<CatalogueMessageEVO>();
            var channels = new HashSet<string>();
            var notifications = new HashSet<string>();

            foreach (NotificationChannelCVO channel in asset.Channels)
            {
                if (string.IsNullOrEmpty(channel.Key))
                    messages.Add(Error(string.Empty, "A channel has no key; it is ignored."));
                else if (!channels.Add(channel.Key))
                    messages.Add(Error(channel.Key, $"Channel '{channel.Key}' is declared twice; the second is ignored."));
                else if (string.IsNullOrEmpty(channel.Name))
                    messages.Add(Warn(channel.Key, $"Channel '{channel.Key}' has no name; the OS settings show it by its key."));
            }

            foreach (NotificationCVO notification in asset.Notifications)
            {
                if (string.IsNullOrEmpty(notification.Key))
                {
                    messages.Add(Error(string.Empty, "A notification has no key; it is ignored."));
                    continue;
                }

                if (!notifications.Add(notification.Key))
                {
                    messages.Add(Error(notification.Key, $"Notification '{notification.Key}' is declared twice; the second is ignored."));
                    continue;
                }

                if (!channels.Contains(notification.Channel ?? string.Empty))
                    messages.Add(Error(notification.Key, $"Notification '{notification.Key}' names channel '{notification.Channel}', which the catalogue does not declare; it is ignored."));

                if (string.IsNullOrEmpty(notification.Title) && string.IsNullOrEmpty(notification.Body))
                    messages.Add(Warn(notification.Key, $"Notification '{notification.Key}' has neither a title nor a body."));

                if (registeredIcons != null)
                {
                    if (!string.IsNullOrEmpty(notification.SmallIcon) && !registeredIcons.Contains(notification.SmallIcon))
                        messages.Add(Warn(notification.Key, $"Notification '{notification.Key}' names small icon '{notification.SmallIcon}', which Project Settings > Mobile Notifications does not register; Android shows the app icon instead."));

                    if (!string.IsNullOrEmpty(notification.LargeIcon) && !registeredIcons.Contains(notification.LargeIcon))
                        messages.Add(Warn(notification.Key, $"Notification '{notification.Key}' names large icon '{notification.LargeIcon}', which Project Settings > Mobile Notifications does not register; Android shows none."));
                }

                if (pictureExists != null && !string.IsNullOrEmpty(notification.Picture) && !pictureExists(notification.Picture))
                    messages.Add(Warn(notification.Key, $"Notification '{notification.Key}' names picture '{notification.Picture}', which is not under Assets/StreamingAssets; it goes out without a picture."));
            }

            for (int index = 0; index < asset.ReturnReminders.Count; index++)
            {
                ReturnReminderCVO reminder = asset.ReturnReminders[index];
                string about = ReminderAbout(index);

                if (!notifications.Contains(reminder.Notification ?? string.Empty))
                    messages.Add(Error(about, $"Return reminder {index + 1} names notification '{reminder.Notification}', which the catalogue does not declare; it is ignored."));

                if (reminder.AfterMinutes <= 0f)
                    messages.Add(Error(about, $"Return reminder {index + 1} has no AfterMinutes; it is ignored."));
            }

            return messages;
        }

        /// <summary>What the sidebar and the messages call the reminder at <paramref name="index"/>.</summary>
        public string ReminderAbout(int index) => "reminder-" + index;

        /// <summary>
        /// Minutes as a person reads them: 1440 is "1 day", 90 is "1.5 hours", 0.2 is "12 seconds".
        /// </summary>
        public string Minutes(float minutes)
        {
            if (minutes <= 0f)
                return "no time";

            if (minutes < 1f)
                return Count(minutes * 60f, "second");

            if (minutes < 60f)
                return Count(minutes, "minute");

            if (minutes < 1440f)
                return Count(minutes / 60f, "hour");

            return Count(minutes / 1440f, "day");
        }

        /// <summary>The sample arguments the panel's field holds, split on commas and trimmed.</summary>
        public object[] ParseArgs(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<object>();

            string[] parts = text.Split(',');
            var args = new object[parts.Length];

            for (int i = 0; i < parts.Length; i++)
                args[i] = parts[i].Trim();

            return args;
        }

        /// <summary>
        /// A template's title and body with the placeholders filled, or the reason they cannot be:
        /// the same string.Format the command runs, so what the preview shows is what the phone
        /// gets.
        /// </summary>
        public bool TryFormat(NotificationCVO template, object[] args, out string title, out string body, out string error)
        {
            title = string.Empty;
            body = string.Empty;
            error = null;

            try
            {
                title = Format(template.Title, args);
                body = Format(template.Body, args);
                return true;
            }
            catch (FormatException)
            {
                error = $"The texts have a placeholder the {args.Length} sample argument(s) do not fill.";
                return false;
            }
        }

        public void AddChannel(SerializedObject serialized)
        {
            SerializedProperty channels = serialized.FindProperty(CHANNELS);
            int index = channels.arraySize;
            channels.InsertArrayElementAtIndex(index);
            SerializedProperty channel = channels.GetArrayElementAtIndex(index);
            channel.FindPropertyRelative(KEY).stringValue = "Channel" + (index + 1);
            channel.FindPropertyRelative(NAME).stringValue = "Channel " + (index + 1);
            channel.FindPropertyRelative(DESCRIPTION).stringValue = string.Empty;
            channel.FindPropertyRelative(IMPORTANCE).enumValueIndex = 1;
        }

        public void AddNotification(SerializedObject serialized, string channelKey)
        {
            SerializedProperty notifications = serialized.FindProperty(NOTIFICATIONS);
            int index = notifications.arraySize;
            notifications.InsertArrayElementAtIndex(index);
            SerializedProperty notification = notifications.GetArrayElementAtIndex(index);
            notification.FindPropertyRelative(KEY).stringValue = "Notification" + (index + 1);
            notification.FindPropertyRelative(CHANNEL).stringValue = channelKey ?? string.Empty;
            notification.FindPropertyRelative(TITLE).stringValue = "Title";
            notification.FindPropertyRelative(BODY).stringValue = "Body";
            notification.FindPropertyRelative(SMALL_ICON).stringValue = string.Empty;
            notification.FindPropertyRelative(LARGE_ICON).stringValue = string.Empty;
            notification.FindPropertyRelative(PICTURE).stringValue = string.Empty;
            notification.FindPropertyRelative(SHOW_IN_FOREGROUND).boolValue = false;
            notification.FindPropertyRelative(REPEATS).boolValue = false;
            notification.FindPropertyRelative(DEFAULT_AFTER_MINUTES).floatValue = 0f;
        }

        public void AddReminder(SerializedObject serialized, string notificationKey)
        {
            SerializedProperty reminders = serialized.FindProperty(RETURN_REMINDERS);
            int index = reminders.arraySize;
            reminders.InsertArrayElementAtIndex(index);
            SerializedProperty reminder = reminders.GetArrayElementAtIndex(index);
            reminder.FindPropertyRelative(NOTIFICATION).stringValue = notificationKey ?? string.Empty;
            reminder.FindPropertyRelative(AFTER_MINUTES).floatValue = 1440f;
        }

        public void Remove(SerializedObject serialized, string listName, int index)
        {
            SerializedProperty list = serialized.FindProperty(listName);

            if (index >= 0 && index < list.arraySize)
                list.DeleteArrayElementAtIndex(index);
        }

        private string Format(string text, object[] args) =>
            string.IsNullOrEmpty(text) ? string.Empty : string.Format(text, args ?? Array.Empty<object>());

        private string Count(float value, string unit)
        {
            float rounded = (float) Math.Round(value, 1);
            string number = rounded.ToString("0.#", CultureInfo.InvariantCulture);

            return number + " " + unit + (Math.Abs(rounded - 1f) < 0.001f ? string.Empty : "s");
        }

        private CatalogueMessageEVO Error(string about, string text) =>
            new() {About = about, Text = text, IsError = true};

        private CatalogueMessageEVO Warn(string about, string text) =>
            new() {About = about, Text = text, IsError = false};
    }
}

#endif
