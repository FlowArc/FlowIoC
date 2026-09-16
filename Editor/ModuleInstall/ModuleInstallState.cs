#if UNITY_EDITOR

using System.Collections.Generic;
using FlowIoC.Editor.Help.WhatsNew;
using FlowIoC.Editor.ModuleCards;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What a module page's button reads and whether it can be pressed, worked out from readings
    /// taken every repaint: whether the page has a package behind it, whether the module is
    /// already in the project, which of the assemblies it needs are absent, and the two versions
    /// - the installed card's and the shipped card's.
    ///
    /// Missing packages are not among them. A package can be added, and the install carries on
    /// once it arrives; a paid asset cannot, so it is the only requirement that stops the button.
    /// </summary>
    internal class ModuleInstallState
    {
        private readonly bool _payloadResolved;
        private readonly bool _installed;
        private readonly IReadOnlyList<string> _missingAssemblies;
        private readonly string _installedVersion;
        private readonly string _shippedVersion;
        private readonly VersionOrder _order = new VersionOrder();

        internal ModuleInstallState(
            bool payloadResolved, bool installed, IReadOnlyList<string> missingAssemblies)
            : this(payloadResolved, installed, missingAssemblies, null, null)
        {
        }

        internal ModuleInstallState(
            bool payloadResolved, bool installed, IReadOnlyList<string> missingAssemblies,
            string installedVersion, string shippedVersion)
        {
            _payloadResolved = payloadResolved;
            _installed = installed;
            _missingAssemblies = missingAssemblies ?? new string[0];
            _installedVersion = installedVersion ?? ModuleCardVersionLine.NONE;
            _shippedVersion = shippedVersion ?? ModuleCardVersionLine.NONE;
        }

        internal bool Installed => _installed;

        internal string ShippedVersion => _shippedVersion;

        /// <summary>
        /// Installed, and the package ships a newer version. A shipped copy with no version at all
        /// offers nothing: 0.0.0 is newer than nothing else.
        /// </summary>
        internal bool UpdateAvailable =>
            _installed && HasShippedVersion && _order.IsNewer(_shippedVersion, _installedVersion);

        internal string Label
        {
            get
            {
                if (_installed)
                    return UpdateAvailable ? "Update to " + _shippedVersion : "Installed";

                if (!_payloadResolved)
                    return "Unavailable";

                return _missingAssemblies.Count > 0 ? "Missing" : "Install";
            }
        }

        internal bool Enabled =>
            UpdateAvailable || (!_installed && _payloadResolved && _missingAssemblies.Count == 0);

        /// <summary>
        /// What the page says above its body, or null when there is nothing to say. For a module
        /// that is not here it names the assemblies rather than the products they come from: the
        /// assembly name is what the module's asmdef references and what the reader has to end up
        /// with. For a module that is here it speaks only where the banner's number and the button
        /// do not explain themselves.
        /// </summary>
        internal string Note
        {
            get
            {
                if (_installed)
                    return InstalledNote;

                if (!_payloadResolved)
                    return "This page is not compiled from a package, so the module it installs "
                           + "cannot be found. A module and the page that installs it "
                           + "ship in the same package.";

                if (_missingAssemblies.Count == 0)
                    return null;

                return "This module references "
                       + string.Join(", ", _missingAssemblies)
                       + ", which this project does not have. Import the asset that brings it "
                       + "before installing the module.";
            }
        }

        /// <summary>
        /// The number the banner shows beside the button: the installed version, or the shipped
        /// one while the module is not here, so a reader sees what they have or what they would
        /// get. Null when neither card carries a version.
        /// </summary>
        internal string Version
        {
            get
            {
                if (_installed)
                    return HasInstalledVersion ? _installedVersion : null;

                return HasShippedVersion ? _shippedVersion : null;
            }
        }

        /// <summary>
        /// The banner already says which version is here and the button which one is offered,
        /// so the note speaks only where the two numbers do not explain themselves: a copy
        /// installed before it carried a version, and a copy ahead of what the package ships.
        /// </summary>
        private string InstalledNote
        {
            get
            {
                if (!HasInstalledVersion)
                    return UpdateAvailable ? "Installed before the module carried a version." : null;

                if (HasShippedVersion && _order.IsNewer(_installedVersion, _shippedVersion))
                    return "Installed " + _installedVersion + ", ahead of the shipped " + _shippedVersion;

                return null;
            }
        }

        private bool HasInstalledVersion => _installedVersion != ModuleCardVersionLine.NONE;

        private bool HasShippedVersion => _shippedVersion != ModuleCardVersionLine.NONE;
    }
}

#endif