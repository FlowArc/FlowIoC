#if UNITY_EDITOR

using System.Collections.Generic;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// What a private module's Install button reads and whether it can be pressed, worked out
    /// from three readings taken every repaint: whether the page has a package behind it, whether
    /// the module is already in the project, and which of the assemblies it needs are absent.
    ///
    /// Missing packages are not among them. A package can be added, and the install carries on
    /// once it arrives; a paid asset cannot, so it is the only requirement that stops the button.
    /// </summary>
    internal class PrivateModuleInstallState
    {
        private readonly bool _payloadResolved;
        private readonly bool _installed;
        private readonly IReadOnlyList<string> _missingAssemblies;

        internal PrivateModuleInstallState(
            bool payloadResolved, bool installed, IReadOnlyList<string> missingAssemblies)
        {
            _payloadResolved = payloadResolved;
            _installed = installed;
            _missingAssemblies = missingAssemblies ?? new string[0];
        }

        internal string Label
        {
            get
            {
                if (_installed)
                    return "Installed";

                if (!_payloadResolved)
                    return "Unavailable";

                return _missingAssemblies.Count > 0 ? "Missing" : "Install";
            }
        }

        internal bool Enabled =>
            !_installed && _payloadResolved && _missingAssemblies.Count == 0;

        /// <summary>
        /// What the page says above its body, or null when there is nothing to say. It names the
        /// assemblies rather than the products they come from: the assembly name is what the
        /// module's asmdef references and what the reader has to end up with.
        /// </summary>
        internal string Note
        {
            get
            {
                if (_installed)
                    return null;

                if (!_payloadResolved)
                    return "This page is not compiled from a package, so the module it installs "
                           + "cannot be found. A private module and the page that installs it "
                           + "ship in the same package.";

                if (_missingAssemblies.Count == 0)
                    return null;

                return "This module references "
                       + string.Join(", ", _missingAssemblies)
                       + ", which this project does not have. Import the asset that brings it "
                       + "before installing the module.";
            }
        }
    }
}

#endif
