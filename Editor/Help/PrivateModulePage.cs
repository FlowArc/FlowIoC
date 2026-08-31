#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// A module that ships in a package of its own rather than in FlowIoC. Some modules are built
    /// on paid assets and cannot sit in a public repository, so they live in a private package -
    /// FlowIoC-addons, or any other - which writes a page like this one for each of them. The
    /// Help window collects them into a Private Modules category under Modules.
    ///
    /// This class declares and does not act. Resolving where the module's files are, checking
    /// whether it is already installed, adding packages it needs, copying it in and reporting the
    /// result are all done by the adapter FlowIoC wraps this in. Keeping the declaration inert is
    /// what lets the machinery behind it change without breaking a page written years ago.
    ///
    /// Every member is public rather than protected: the adapter lives in another assembly and
    /// cannot read protected members, and a protected override paired with a public accessor for
    /// each one would double the surface to hide nothing.
    /// </summary>
    public abstract class PrivateModulePage
    {
        /// <summary>What the sidebar calls this module.</summary>
        public abstract string Title { get; }

        /// <summary>A second line under the title, for a name that does not say enough alone.</summary>
        public virtual string Subtitle => string.Empty;

        /// <summary>
        /// The built-in Editor icon drawn beside the topic, by the name
        /// EditorGUIUtility.IconContent takes. Skin-neutral names only: Unity picks the dark
        /// variant itself.
        /// </summary>
        public virtual string Icon => "Prefab Icon";

        /// <summary>
        /// The folder under PrivateModules~ this page installs, inside the package the page
        /// itself ships in. The package is not named anywhere: it is read from this class's own
        /// assembly, so a page and the module it installs always travel together.
        /// </summary>
        public abstract string ModuleFolderName { get; }

        /// <summary>
        /// Assemblies the module's asmdefs reference that no package brings - a paid asset
        /// imported into Assets. The Install button stays disabled until they are all here,
        /// because copying the module in without them stops the project compiling.
        /// </summary>
        public virtual IReadOnlyList<string> RequiredAssemblies => Array.Empty<string>();

        /// <summary>
        /// Package Manager ids the module needs. Missing ones are offered for adding before the
        /// module is copied, the way the camera module's Cinemachine requirement already is.
        /// </summary>
        public virtual IReadOnlyList<string> RequiredPackages => Array.Empty<string>();

        /// <summary>What the first reading of the page is called.</summary>
        public virtual string BodyTabTitle => "Introduction";

        /// <summary>Readings beside the body. Empty for a page that is one reading only.</summary>
        public virtual IReadOnlyList<HelpTab> MoreTabs => Array.Empty<HelpTab>();

        /// <summary>The body of the page, drawn with the same marks every other page uses.</summary>
        public abstract void DrawBody(HelpPainter painter);
    }
}

#endif
