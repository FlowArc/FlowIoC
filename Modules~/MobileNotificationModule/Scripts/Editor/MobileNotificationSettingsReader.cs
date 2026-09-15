#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Notifications;
using UnityEditor;
using UnityEngine;

namespace Modules.MobileNotificationModule.Editor
{
    /// <summary>
    /// What Project Settings > Mobile Notifications says, read for the panel: the Android icons a
    /// catalogue may name, and the two switches that fail silently when left wrong. The package
    /// publishes the switches but not the icon list - only Add and Remove - so the list is read
    /// off its settings manager by reflection, the one coupling here, and comes back empty if
    /// the package ever renames it.
    /// </summary>
    internal class MobileNotificationSettingsReader
    {
        private const string SETTINGS_PATH = "Project/Mobile Notifications";
        private const string MANAGER_TYPE = "Unity.Notifications.NotificationSettingsManager";
        private const string INITIALIZE = "Initialize";
        private const string DRAWABLES = "DrawableResources";
        private const string DRAWABLE_ID = "Id";
        private const string DRAWABLE_ASSET = "Asset";

        public bool RescheduleOnDeviceRestart => NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart;

        public bool RequestAuthorizationOnAppLaunch => NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch;

        /// <summary>The registered Android icons by name, with their textures for the preview.</summary>
        public Dictionary<string, Texture2D> Icons()
        {
            var icons = new Dictionary<string, Texture2D>();

            System.Type managerType = typeof(NotificationSettings).Assembly.GetType(MANAGER_TYPE);
            MethodInfo initialize = managerType?.GetMethod(INITIALIZE,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object manager = initialize?.Invoke(null, null);
            FieldInfo drawables = managerType?.GetField(DRAWABLES, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (manager == null || drawables == null || drawables.GetValue(manager) is not IEnumerable list)
                return icons;

            foreach (object drawable in list)
            {
                if (drawable == null) continue;

                System.Type type = drawable.GetType();
                var id = type.GetField(DRAWABLE_ID)?.GetValue(drawable) as string;
                var asset = type.GetField(DRAWABLE_ASSET)?.GetValue(drawable) as Texture2D;

                if (!string.IsNullOrEmpty(id) && !icons.ContainsKey(id))
                    icons.Add(id, asset);
            }

            return icons;
        }

        public void OpenSettings() => SettingsService.OpenProjectSettings(SETTINGS_PATH);
    }
}

#endif
