#if UNITY_EDITOR
using System.Xml;

namespace Modules.HapticModule.Editor
{
    /// <summary>
    /// Vibrating needs android.permission.VIBRATE, and Unity adds it on its own only when it sees
    /// Handheld.Vibrate() in the code. The module calls the Vibrator through JNI, so it declares
    /// the permission itself - once, whatever the game or another plugin already put there.
    /// Works on the manifest's text, because Unity 6's typed manifest API lets a build step add
    /// files of its own but not edit the unityLibrary manifest it generated.
    /// </summary>
    public class HapticAndroidManifest
    {
        private const string VIBRATE = "android.permission.VIBRATE";
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";

        /// <summary>
        /// The manifest with the permission in it. Text that already carries it, or that is not a
        /// manifest at all, comes back exactly as it was.
        /// </summary>
        public string AddVibratePermission(string manifestXml)
        {
            var document = new XmlDocument {PreserveWhitespace = true};

            try
            {
                document.LoadXml(manifestXml);
            }
            catch (XmlException)
            {
                return manifestXml;
            }

            XmlElement manifest = document.DocumentElement;

            if (manifest == null || manifest.Name != "manifest")
                return manifestXml;

            foreach (XmlNode node in manifest.ChildNodes)
            {
                if (node is XmlElement {Name: "uses-permission"} element
                    && element.GetAttribute("name", ANDROID_NAMESPACE) == VIBRATE)
                    return manifestXml;
            }

            XmlElement permission = document.CreateElement("uses-permission");
            permission.SetAttribute("name", ANDROID_NAMESPACE, VIBRATE);

            XmlNode first = manifest.FirstChild;
            manifest.InsertBefore(document.CreateTextNode("\n  "), first);
            manifest.InsertBefore(permission, first);

            return document.OuterXml;
        }
    }
}
#endif