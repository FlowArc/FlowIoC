#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Root;
using FlowIoC.Editor.ModulePanels;
using Modules.MobileNotificationModule.Data.UnityObjects;
using Modules.MobileNotificationModule.Data.ValueObjects;
using Modules.MobileNotificationModule.RootsContexts;
using Modules.MobileNotificationModule.Services;
using UnityEditor;
using System.IO;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.MobileNotificationModule.Editor
{
    /// <summary>
    /// The catalogue as the developer works on it, and the card the phone will show for it. The
    /// left side lists the templates, the channels and the return reminders of the catalogue -
    /// and, while the game runs, what the service has scheduled; the right side edits the one
    /// picked and previews it as Android's and iOS's trays would draw it, sample arguments filling
    /// its placeholders. It also reads the three Mobile Notifications settings that fail silently
    /// when left wrong, and says so above everything else.
    ///
    /// Editing the catalogue is authoring, the way the A/B Test editor authors its asset; the
    /// running service's state is only read.
    /// </summary>
    internal class MobileNotificationPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Mobile Notification/Panel";

        /// <summary>
        /// Between Tools/FlowIoC, whose items sit around -1300 to -1100, and Tools/FlowIoC-dev at
        /// -1050: a submenu takes its place from its lowest item, so this is what keeps
        /// FlowIoC-Modules the second entry under Tools.
        /// </summary>
        private const int MENU_PRIORITY = -1080;

        private const string NOW = "now";
        private const string ICON_NONE = "(none)";

        private static readonly Color AndroidAccent = new(0.62f, 0.76f, 1f);

        private enum Picked
        {
            Notification = 0,
            Channel = 1,
            Reminder = 2,
            Scheduled = 3
        }

        private readonly MobileNotificationCatalogueTools _tools = new();
        private readonly MobileNotificationSettingsReader _settings = new();
        private readonly NotificationCardPainter _cards = new();
        private readonly Queue<Action> _pending = new();

        private Picked _picked = Picked.Notification;
        private int _index;
        private string _sampleArgs = "Gold";
        private Dictionary<string, Texture2D> _icons;
        private double _iconsReadAt = double.NegativeInfinity;
        private readonly Dictionary<string, Texture2D> _pictures = new();
        private readonly Dictionary<string, DateTime> _pictureStamps = new();

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<MobileNotificationPanel>();

        public override string Title => "Mobile Notification";

        public override string Module => "MobileNotificationModule";

        public override string Subtitle => "The catalogue, and the card the phone will show for it";

        public override FlowRole Role => FlowRole.Service;

        public override string HelpPage => "Mobile Notification";

        public override bool HasSidebar => true;

        public override ModulePanelAction? BarAction
        {
            get
            {
                CD_MobileNotifications asset = Asset();

                return asset == null
                    ? null
                    : new ModulePanelAction("Select asset", () => Select(asset), ModulePanelActionKind.Plain);
            }
        }

        public override void DrawSidebar(ModulePanelSidebarPainter sidebar)
        {
            CD_MobileNotifications asset = Asset();

            if (asset == null)
                return;

            SerializedObject serialized = _tools.Serialized(asset);

            sidebar.Heading("Notifications", ModulePanelAction.Add("+", () => Defer(() => AddNotification(serialized, asset))));

            for (int i = 0; i < asset.Notifications.Count; i++)
            {
                int index = i;
                NotificationCVO notification = asset.Notifications[i];
                sidebar.Item(Label(notification.Key), Is(Picked.Notification, i), null, () => Pick(Picked.Notification, index));
            }

            sidebar.Space();
            sidebar.Heading("Channels", ModulePanelAction.Add("+", () => Defer(() => _tools.AddChannel(serialized))));

            for (int i = 0; i < asset.Channels.Count; i++)
            {
                int index = i;
                sidebar.Item(Label(asset.Channels[i].Key), Is(Picked.Channel, i), null, () => Pick(Picked.Channel, index));
            }

            sidebar.Space();
            sidebar.Heading("Return reminders", ModulePanelAction.Add("+", () => Defer(() => AddReminder(serialized, asset))));

            for (int i = 0; i < asset.ReturnReminders.Count; i++)
            {
                int index = i;
                ReturnReminderCVO reminder = asset.ReturnReminders[i];
                sidebar.Item(Label(reminder.Notification), Is(Picked.Reminder, i), _tools.Minutes(reminder.AfterMinutes),
                    () => Pick(Picked.Reminder, index));
            }

            IReadOnlyList<NotificationDraftVO> scheduled = Scheduled();

            if (scheduled == null)
                return;

            sidebar.Space();
            sidebar.Heading("Scheduled");

            if (scheduled.Count == 0)
                sidebar.Item("nothing yet", false, null, null);

            for (int i = 0; i < scheduled.Count; i++)
            {
                int index = i;
                NotificationDraftVO draft = scheduled[i];
                sidebar.Item(draft.Identity.Identifier, Is(Picked.Scheduled, i), Countdown(draft.FireTime),
                    () => Pick(Picked.Scheduled, index));
            }
        }

        public override void Draw(ModulePanelPainter painter)
        {
            CD_MobileNotifications asset = Asset();

            if (asset == null)
            {
                painter.Heading("Catalogue");
                painter.Note("No CD_MobileNotifications asset in the project yet. Create one, then file it on "
                             + "MobileNotificationServiceRoot's adapter under CD_MobileNotifications.");
                painter.Actions(new ModulePanelAction("Create CD_MobileNotifications", CreateAsset, ModulePanelActionKind.Confirm));
                return;
            }

            SerializedObject serialized = _tools.Serialized(asset);
            serialized.Update();

            Dictionary<string, Texture2D> icons = Icons();
            List<CatalogueMessageEVO> messages = _tools.Messages(asset, icons.Keys, PictureExists);

            DrawSettings(painter);

            switch (_picked)
            {
                case Picked.Notification:
                    DrawNotification(painter, serialized, asset, icons, messages);
                    break;
                case Picked.Channel:
                    DrawChannel(painter, serialized, asset, messages);
                    break;
                case Picked.Reminder:
                    DrawReminder(painter, serialized, asset, messages);
                    break;
                case Picked.Scheduled:
                    DrawScheduled(painter, icons);
                    break;
            }

            DrawMessages(painter, messages, string.Empty);

            serialized.ApplyModifiedProperties();

            while (_pending.Count > 0)
                _pending.Dequeue()();
        }

        // --- the project settings that fail silently ---

        private void DrawSettings(ModulePanelPainter painter)
        {
            bool reschedule = _settings.RescheduleOnDeviceRestart;
            bool askOnLaunch = _settings.RequestAuthorizationOnAppLaunch;
            int icons = Icons().Count;

            painter.Heading("Project settings",
                new ModulePanelAction("Open Mobile Notifications", _settings.OpenSettings, ModulePanelActionKind.Plain));

            if (!reschedule)
                painter.Warning("Android > Reschedule on Device Restart is off: a reboot forgets every scheduled notification.");

            if (askOnLaunch)
                painter.Warning("iOS > Request Authorization on App Launch is on: it pre-empts the RequestPermission step. Turn it off.");

            if (icons == 0)
                painter.Warning("Android > Notification Icons is empty: every notification shows the app icon.");

            if (reschedule && !askOnLaunch && icons > 0)
                painter.Note($"Reschedule on Device Restart on, iOS ask on launch off, {icons} Android icon(s) registered.");

            painter.Space();
        }

        // --- a notification template ---

        private void DrawNotification(ModulePanelPainter painter, SerializedObject serialized, CD_MobileNotifications asset,
            Dictionary<string, Texture2D> icons, List<CatalogueMessageEVO> messages)
        {
            SerializedProperty list = serialized.FindProperty(MobileNotificationCatalogueTools.NOTIFICATIONS);

            if (!Clamp(list.arraySize))
            {
                painter.Heading("Notifications");
                painter.Note("No notification yet. The + on the left adds one.");
                return;
            }

            SerializedProperty notification = list.GetArrayElementAtIndex(_index);
            SerializedProperty key = notification.FindPropertyRelative(MobileNotificationCatalogueTools.KEY);
            NotificationCVO template = asset.Notifications[_index];

            painter.Heading(Label(key.stringValue),
                new ModulePanelAction("Remove", () => Defer(() => Remove(serialized, MobileNotificationCatalogueTools.NOTIFICATIONS)),
                    ModulePanelActionKind.Remove));

            painter.Property(key, "Key", "What a game names to schedule it: _notifications.Schedule(\"" + key.stringValue + "\", ...).");
            DrawChannelPopup(painter, notification.FindPropertyRelative(MobileNotificationCatalogueTools.CHANNEL), asset);
            painter.Property(notification.FindPropertyRelative(MobileNotificationCatalogueTools.TITLE), "Title",
                "May carry {0}, {1} placeholders that the caller's args fill.");
            // The body is a TextArea on the asset; the panel row is one line, and a notification body is a sentence.
            SerializedProperty body = notification.FindPropertyRelative(MobileNotificationCatalogueTools.BODY);
            string typed = painter.TextField("Body", body.stringValue, false, "May carry {0}, {1} placeholders that the caller's args fill.");
            if (typed != body.stringValue) body.stringValue = typed;
            DrawIconPopup(painter, notification.FindPropertyRelative(MobileNotificationCatalogueTools.SMALL_ICON), "Small icon", icons,
                "Android's status bar glyph, an alpha mask tinted by the OS. Registered under Project Settings > Mobile Notifications; iOS ignores it.");
            DrawIconPopup(painter, notification.FindPropertyRelative(MobileNotificationCatalogueTools.LARGE_ICON), "Large icon", icons,
                "The picture at the right of the Android card. iOS ignores it.");
            SerializedProperty picture = notification.FindPropertyRelative(MobileNotificationCatalogueTools.PICTURE);
            string typedPicture = painter.TextField("Picture", picture.stringValue, false,
                "An image file under Assets/StreamingAssets, as Notifications/chest.png. Android shows it under the text when the notification is expanded; iOS as the banner's thumbnail and the full image on a long press. Empty for none.");
            if (typedPicture != picture.stringValue) picture.stringValue = typedPicture;
            painter.Property(notification.FindPropertyRelative(MobileNotificationCatalogueTools.SHOW_IN_FOREGROUND), "Show in foreground",
                "Whether the tray shows it while the game is on screen. Off for a reminder the player is already answering.");
            painter.Property(notification.FindPropertyRelative(MobileNotificationCatalogueTools.REPEATS), "Repeats",
                "At the interval it was scheduled with: scheduled after a delay, every such delay; scheduled at a time, daily at that time. Under 60 s is refused.");
            painter.Property(notification.FindPropertyRelative(MobileNotificationCatalogueTools.DEFAULT_AFTER_MINUTES), "Default after (minutes)",
                "The time a bound IMobileNotificationService.Commands.Schedule step uses; 0 means the caller always gives one. 1440 is a day.");

            DrawMessages(painter, messages, template.Key);

            painter.Space();
            DrawPreview(painter, template, icons);
        }

        private void DrawChannelPopup(ModulePanelPainter painter, SerializedProperty channel, CD_MobileNotifications asset)
        {
            var options = new string[asset.Channels.Count];
            int selected = -1;

            for (int i = 0; i < options.Length; i++)
            {
                options[i] = Label(asset.Channels[i].Key);

                if (asset.Channels[i].Key == channel.stringValue)
                    selected = i;
            }

            if (options.Length == 0 || selected < 0)
            {
                painter.Property(channel, "Channel",
                    "The category the player sees in the OS settings; one of the catalogue's channels.");
                return;
            }

            int picked = painter.Popup("Channel", selected, options,
                "The category the player sees in the OS settings; one of the catalogue's channels.");

            if (picked != selected)
                channel.stringValue = asset.Channels[picked].Key;
        }

        private void DrawIconPopup(ModulePanelPainter painter, SerializedProperty icon, string label,
            Dictionary<string, Texture2D> icons, string help)
        {
            var options = new List<string> {ICON_NONE};
            options.AddRange(icons.Keys);

            int selected = string.IsNullOrEmpty(icon.stringValue) ? 0 : options.IndexOf(icon.stringValue);

            // A name the settings do not know is kept as typed and reported below, not silently replaced.
            if (selected < 0)
            {
                painter.Property(icon, label, help);
                return;
            }

            int picked = painter.Popup(label, selected, options.ToArray(), help);

            if (picked != selected)
                icon.stringValue = picked == 0 ? string.Empty : options[picked];
        }

        // --- a channel ---

        private void DrawChannel(ModulePanelPainter painter, SerializedObject serialized, CD_MobileNotifications asset,
            List<CatalogueMessageEVO> messages)
        {
            SerializedProperty list = serialized.FindProperty(MobileNotificationCatalogueTools.CHANNELS);

            if (!Clamp(list.arraySize))
            {
                painter.Heading("Channels");
                painter.Note("No channel yet. The + on the left adds one; every notification names one.");
                return;
            }

            SerializedProperty channel = list.GetArrayElementAtIndex(_index);
            SerializedProperty key = channel.FindPropertyRelative(MobileNotificationCatalogueTools.KEY);
            int used = 0;

            foreach (NotificationCVO notification in asset.Notifications)
                if (notification.Channel == key.stringValue)
                    used++;

            painter.Heading(Label(key.stringValue) + " - channel",
                new ModulePanelAction("Remove", () => Defer(() => Remove(serialized, MobileNotificationCatalogueTools.CHANNELS)),
                    ModulePanelActionKind.Remove));

            painter.Property(key, "Key", "What a notification names as its Channel. Registered on Android under this id at initialize.");
            painter.Property(channel.FindPropertyRelative(MobileNotificationCatalogueTools.NAME), "Name",
                "What the player reads in the OS notification settings, where each channel can be muted on its own.");
            painter.Property(channel.FindPropertyRelative(MobileNotificationCatalogueTools.DESCRIPTION), "Description");
            painter.Property(channel.FindPropertyRelative(MobileNotificationCatalogueTools.IMPORTANCE), "Importance",
                "Android: High pops over the screen with sound, Default makes a sound, Low stays silent in the tray. iOS ignores it.");
            painter.Field("Used by", used == 1 ? "1 notification" : used + " notifications");

            DrawMessages(painter, messages, key.stringValue);
        }

        // --- a return reminder ---

        private void DrawReminder(ModulePanelPainter painter, SerializedObject serialized, CD_MobileNotifications asset,
            List<CatalogueMessageEVO> messages)
        {
            SerializedProperty list = serialized.FindProperty(MobileNotificationCatalogueTools.RETURN_REMINDERS);

            if (!Clamp(list.arraySize))
            {
                painter.Heading("Return reminders");
                painter.Note(
                    "No reminder yet. The + on the left adds one. Each goes out by itself when the app is left and is taken back when it returns.");
                return;
            }

            SerializedProperty reminder = list.GetArrayElementAtIndex(_index);
            SerializedProperty notification = reminder.FindPropertyRelative(MobileNotificationCatalogueTools.NOTIFICATION);
            SerializedProperty minutes = reminder.FindPropertyRelative(MobileNotificationCatalogueTools.AFTER_MINUTES);

            painter.Heading("Return reminder " + (_index + 1),
                new ModulePanelAction("Remove", () => Defer(() => Remove(serialized, MobileNotificationCatalogueTools.RETURN_REMINDERS)),
                    ModulePanelActionKind.Remove));

            DrawNotificationPopup(painter, notification, asset);
            painter.Property(minutes, "After (minutes)",
                "Minutes after the app is left. 1440 is a day, 4320 three days, 10080 a week; 0.2 is twelve seconds for a device test.");
            painter.Field("Reads as", _tools.Minutes(minutes.floatValue));
            painter.Field("Identity", Label(notification.stringValue) + "#" + asset.ReturnReminders[_index].Tag,
                "Three reminders on one template stay apart by their minutes; the same identity scheduled again replaces the earlier one.");

            DrawMessages(painter, messages, _tools.ReminderAbout(_index));

            NotificationCVO template = asset.Notifications.Find(t => t.Key == notification.stringValue);

            if (template != null)
            {
                painter.Space();
                DrawPreview(painter, template, Icons());
            }
        }

        private void DrawNotificationPopup(ModulePanelPainter painter, SerializedProperty notification, CD_MobileNotifications asset)
        {
            var options = new string[asset.Notifications.Count];
            int selected = -1;

            for (int i = 0; i < options.Length; i++)
            {
                options[i] = Label(asset.Notifications[i].Key);

                if (asset.Notifications[i].Key == notification.stringValue)
                    selected = i;
            }

            if (options.Length == 0 || selected < 0)
            {
                painter.Property(notification, "Notification", "The template key the reminder sends.");
                return;
            }

            int picked = painter.Popup("Notification", selected, options, "The template key the reminder sends.");

            if (picked != selected)
                notification.stringValue = asset.Notifications[picked].Key;
        }

        // --- what the running service scheduled ---

        private void DrawScheduled(ModulePanelPainter painter, Dictionary<string, Texture2D> icons)
        {
            IReadOnlyList<NotificationDraftVO> scheduled = Scheduled();

            if (scheduled == null || !Clamp(scheduled.Count))
            {
                painter.Heading("Scheduled");
                painter.Note(
                    "Nothing scheduled. Enter play mode with MobileNotificationServiceRoot in the scene, then schedule from the game or the test scene.");
                return;
            }

            NotificationDraftVO draft = scheduled[_index];

            painter.Heading(draft.Identity.Identifier + " - scheduled");
            painter.Field("Fires at", draft.FireTime.ToString("yyyy-MM-dd HH:mm:ss") + " (" + Countdown(draft.FireTime) + ")");
            painter.Field("Channel", Label(draft.Channel?.Key));
            painter.Field("Repeats", draft.Repeats ? "every " + draft.RepeatInterval : "no");
            painter.Note("What the service remembers this session; the OS is not asked. The Editor gateway logs the delivery when the time passes.");

            painter.Space();
            painter.Heading("Preview");
            DrawCards(painter, Card(draft.Template, draft.Title, draft.Body, draft.Channel?.Name, icons));
        }

        // --- the preview ---

        private object[] ParsedArgs(ModulePanelPainter painter)
        {
            _sampleArgs = painter.TextField("Sample args", _sampleArgs, false,
                "Comma-separated values that stand in for {0}, {1}... in the preview - the args a game passes to Schedule.");

            return _tools.ParseArgs(_sampleArgs);
        }

        private void DrawPreview(ModulePanelPainter painter, NotificationCVO template, Dictionary<string, Texture2D> icons)
        {
            painter.Heading("Preview");
            object[] args = ParsedArgs(painter);

            if (!_tools.TryFormat(template, args, out string title, out string body, out string error))
            {
                painter.Warning(error + " The Schedule call would be refused with the same reason.");
                return;
            }

            NotificationChannelCVO channel = Asset().Channels.Find(c => c.Key == template.Channel);

            DrawCards(painter, Card(template, title, body, channel?.Name, icons));
        }

        private void DrawCards(ModulePanelPainter painter, NotificationCardEVO card)
        {
            painter.Field("Android",
                (card.Picture != null ? "as the tray shows it, expanded - the picture opens under the text" : "as the tray shows it, collapsed") +
                (card.SmallIcon == null ? " - no small icon registered, so the app icon stands in" : string.Empty));
            painter.Custom(_cards.AndroidHeight(card), rect => _cards.Android(rect, card));
            painter.Field("iOS", card.Picture != null ? "as the banner shows it, the picture as its thumbnail" : "as the banner shows it");
            painter.Custom(_cards.IosHeight(card), rect => _cards.Ios(rect, card));
        }

        private NotificationCardEVO Card(NotificationCVO template, string title, string body, string channelName,
            Dictionary<string, Texture2D> icons)
        {
            icons.TryGetValue(template.SmallIcon ?? string.Empty, out Texture2D small);
            icons.TryGetValue(template.LargeIcon ?? string.Empty, out Texture2D large);

            return new NotificationCardEVO
            {
                AppName = PlayerSettings.productName,
                Title = title,
                Body = body,
                Time = NOW,
                Channel = channelName,
                SmallIcon = small,
                LargeIcon = large,
                AppIcon = AppIcon(),
                Picture = Picture(template.Picture),
                Accent = AndroidAccent
            };
        }

        /// <summary>
        /// The picture as authored under Assets/StreamingAssets, decoded for the preview - Unity
        /// keeps that folder as raw files, so there is no texture asset to load. Null when the
        /// template names none, the file is not there, or it is not an image; read again when the
        /// file changes.
        /// </summary>
        private Texture2D Picture(string picture)
        {
            if (string.IsNullOrEmpty(picture) || !PictureExists(picture))
                return null;

            string path = Path.Combine(Application.streamingAssetsPath, picture);
            DateTime stamp = File.GetLastWriteTimeUtc(path);

            if (_pictures.TryGetValue(picture, out Texture2D cached) && cached != null && _pictureStamps[picture] == stamp)
                return cached;

            if (cached != null)
                Object.DestroyImmediate(cached);

            var texture = new Texture2D(2, 2) {hideFlags = HideFlags.HideAndDontSave};

            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Object.DestroyImmediate(texture);
                _pictures.Remove(picture);
                return null;
            }

            _pictures[picture] = texture;
            _pictureStamps[picture] = stamp;
            return texture;
        }

        private bool PictureExists(string picture) =>
            File.Exists(Path.Combine(Application.streamingAssetsPath, picture ?? string.Empty));

        /// <summary>The app icon the phone shows: Android's, else the default one, else Unity's own.</summary>
        private Texture2D AppIcon()
        {
            foreach (NamedBuildTarget target in new[] {NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Unknown})
            {
                Texture2D[] set = PlayerSettings.GetIcons(target, IconKind.Application);

                if (set != null)
                    foreach (Texture2D icon in set)
                        if (icon != null)
                            return icon;
            }

            return EditorGUIUtility.IconContent("UnityLogo").image as Texture2D;
        }

        // --- shared ---

        private void DrawMessages(ModulePanelPainter painter, List<CatalogueMessageEVO> messages, string about)
        {
            foreach (CatalogueMessageEVO message in messages)
            {
                if (message.About != about) continue;

                if (message.IsError)
                    painter.Warning(message.Text);
                else
                    painter.Note(message.Text);
            }
        }

        /// <summary>
        /// The catalogue the game would read: the one on the open scene's MobileNotificationServiceRoot
        /// adapter when there is one, else the first in the project.
        /// </summary>
        private CD_MobileNotifications Asset()
        {
            var root = Object.FindFirstObjectByType<MobileNotificationServiceRoot>();
            RootAdapter adapter = root != null ? root.GetComponent<RootAdapter>() : null;
            CD_MobileNotifications filed = adapter != null ? adapter.GetScriptable<CD_MobileNotifications>() : null;

            if (filed != null)
                return filed;

            List<CD_MobileNotifications> assets = _tools.Assets();

            return assets.Count > 0 ? assets[0] : null;
        }

        /// <summary>The running service's list, or null while nothing runs.</summary>
        private IReadOnlyList<NotificationDraftVO> Scheduled()
        {
            if (!EditorApplication.isPlaying)
                return null;

            IReadOnlyList<NotificationDraftVO> scheduled = null;

            RootsManagerFactory.ExecuteSafelyOnRootsManager(manager =>
            {
                if (manager is not RootsManager roots || !roots.InjectionBinderCrossContext.HasBinding<IMobileNotificationService>())
                    return;

                var service = (IMobileNotificationService) roots.InjectionBinderCrossContext.GetInstance(typeof(IMobileNotificationService));
                scheduled = service?.Scheduled;
            });

            return scheduled;
        }

        private Dictionary<string, Texture2D> Icons()
        {
            // The settings asset is read again at most once a second: a popup redraws every frame.
            if (_icons == null || EditorApplication.timeSinceStartup - _iconsReadAt > 1d)
            {
                _icons = _settings.Icons();
                _iconsReadAt = EditorApplication.timeSinceStartup;
            }

            return _icons;
        }

        private string Countdown(DateTime fireTime)
        {
            TimeSpan left = fireTime - DateTime.Now;

            if (left.TotalSeconds <= 0)
                return "due";

            if (left.TotalMinutes < 1)
                return "in " + (int) left.TotalSeconds + " s";

            return "in " + _tools.Minutes((float) left.TotalMinutes);
        }

        private void AddNotification(SerializedObject serialized, CD_MobileNotifications asset)
        {
            _tools.AddNotification(serialized, asset.Channels.Count > 0 ? asset.Channels[0].Key : string.Empty);
            serialized.ApplyModifiedProperties();
            Pick(Picked.Notification, asset.Notifications.Count - 1);
        }

        private void AddReminder(SerializedObject serialized, CD_MobileNotifications asset)
        {
            _tools.AddReminder(serialized, asset.Notifications.Count > 0 ? asset.Notifications[0].Key : string.Empty);
            serialized.ApplyModifiedProperties();
            Pick(Picked.Reminder, asset.ReturnReminders.Count - 1);
        }

        private void Remove(SerializedObject serialized, string listName)
        {
            _tools.Remove(serialized, listName, _index);
            serialized.ApplyModifiedProperties();
            _index = Mathf.Max(0, _index - 1);
        }

        private void CreateAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create CD_MobileNotifications", "CD_MobileNotifications", "asset",
                "Where the catalogue goes; file it on MobileNotificationServiceRoot's adapter afterwards.");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<CD_MobileNotifications>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Select(asset);
        }

        private static void Select(CD_MobileNotifications asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private bool Is(Picked picked, int index) => _picked == picked && _index == index;

        private void Pick(Picked picked, int index)
        {
            _picked = picked;
            _index = index;
        }

        /// <summary>Keeps the index inside the list it points at; false when the list is empty.</summary>
        private bool Clamp(int count)
        {
            if (count == 0)
                return false;

            _index = Mathf.Clamp(_index, 0, count - 1);
            return true;
        }

        /// <summary>A structural change is done after the rows are drawn, or the layout pass reads a list that just changed under it.</summary>
        private void Defer(Action action) => _pending.Enqueue(action);

        private static string Label(string key) => string.IsNullOrEmpty(key) ? "(no key)" : key;
    }
}

#endif