#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.PackageManager;

namespace FlowIoC.Editor.ModuleInstall
{
    /// <summary>
    /// The packages this project has resolved, by id. The Package Manager answers this one
    /// synchronously - unlike Client.List, which would make every repaint of an install button
    /// wait on a request - so a page can ask it while it draws.
    ///
    /// It exists so that MissingPackages stays free of Unity and can be tested on its own.
    /// </summary>
    internal class InstalledPackages
    {
        internal IReadOnlyList<string> Ids()
        {
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();
            var ids = new List<string>(packages.Length);

            foreach (PackageInfo package in packages)
                ids.Add(package.name);

            return ids;
        }
    }
}

#endif
