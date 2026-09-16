#if UNITY_EDITOR
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// What the What's New tab says about the latest release, and how it says it. The rule holds
    /// nothing of the Editor beyond the enum that names where a package came from, so the cases
    /// can be read here rather than reproduced by installing the package five ways.
    ///
    /// The line is passive on purpose. It tells the reader a release is out and how the install
    /// they have is updated; it never offers to do it, because changing the package under a
    /// project is the developer's call and the Package Manager is where they make it.
    /// </summary>
    internal class UpdateNoticeRule
    {
        private readonly VersionOrder _order = new VersionOrder();

        /// <summary>
        /// <paramref name="installed"/> is empty when the package is not resolved through the
        /// Package Manager, and <paramref name="latest"/> is empty until the registry has
        /// answered - both are nothing to say. <paramref name="source"/> is how the package got
        /// into the project, which decides the sentence about updating it.
        /// </summary>
        internal UpdateNoticeEVO For(string installed, PackageSource source, string latest)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest))
                return new UpdateNoticeEVO(UpdateNoticeKind.None, string.Empty);

            if (_order.IsNewer(latest, installed))
            {
                return new UpdateNoticeEVO(UpdateNoticeKind.Behind,
                    ($"FlowIoC {latest} is out; this project is on {installed}. " + HowToUpdate(source)).TrimEnd());
            }

            string text = _order.IsNewer(installed, latest)
                ? $"{installed} is ahead of the latest release, {latest}."
                : $"{installed} is the latest release.";

            return new UpdateNoticeEVO(UpdateNoticeKind.UpToDate, text);
        }

        /// <summary>
        /// The Package Manager offers an update for a registry install and for nothing else, so
        /// every other road says where the version is actually changed. An embedded copy is the
        /// one that surprises people: the folder under Packages/ wins over whatever the manifest
        /// says, and the window shows no update because there is no list to update from.
        /// </summary>
        private static string HowToUpdate(PackageSource source)
        {
            switch (source)
            {
                case PackageSource.Registry:
                    return "Update it from Window > Package Manager.";

                case PackageSource.Git:
                    return "The Package Manager does not update a Git install: change the tag on "
                           + "the URL in Packages/manifest.json.";

                case PackageSource.Embedded:
                    return "The Package Manager does not update an embedded copy: replace the "
                           + "folder under Packages/, or delete it and install from the registry.";

                case PackageSource.Local:
                case PackageSource.LocalTarball:
                    return "The Package Manager does not update a local install: point "
                           + "Packages/manifest.json at the newer copy.";

                default:
                    return string.Empty;
            }
        }
    }
}

#endif
