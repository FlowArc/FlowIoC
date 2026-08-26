#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// The screenshots the help pages draw, loaded on the first draw that asks for one and kept
    /// for as long as the page lives. The package root is resolved from this assembly rather than
    /// hardcoded, so a picture is found however the package was installed - embedded under
    /// Packages/, pulled from a Git URL into Library/PackageCache, or resolved from a registry.
    /// </summary>
    internal class HelpImages
    {
        private const string FALLBACK_ROOT = "Packages/com.flowarc.flowioc.core";

        private readonly Dictionary<string, Texture2D> _loaded = new Dictionary<string, Texture2D>();

        /// <summary>
        /// The picture of that name under Editor/Help/Images, or null when the project no longer
        /// ships it. A page draws nothing rather than failing over a missing screenshot.
        /// </summary>
        public Texture2D Get(string fileName)
        {
            if (_loaded.TryGetValue(fileName, out Texture2D cached) && cached != null)
                return cached;

            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HelpImages).Assembly);
            string root = package != null ? package.assetPath : FALLBACK_ROOT;

            var image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{root}/Editor/Help/Images/{fileName}");
            _loaded[fileName] = image;

            return image;
        }
    }
}

#endif