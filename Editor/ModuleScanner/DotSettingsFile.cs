#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml;
using FlowIoC.Editor.CodeGenerator.Menus.Module;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// One .csproj.DotSettings file, compared against a plan or written from it. Everything about
    /// the XML format itself stays in NamespaceUtility, which already owns it; this class only
    /// decides whether the file on disk says what the plan says it should.
    ///
    /// The methods are virtual so a test can stand in for the disk - the file is the one part of
    /// DotSettingsCheck that cannot be described in a fixture.
    /// </summary>
    internal class DotSettingsFile
    {
        internal virtual bool Matches(string path, IReadOnlyList<string> skipFolders)
        {
            if (!File.Exists(path)) return false;

            string content = File.ReadAllText(path);

            foreach (string folder in skipFolders)
            {
                if (!content.Contains(NamespaceUtility.EncodeAssetPath(folder))) return false;
            }

            return true;
        }

        internal virtual void Write(string path, IReadOnlyList<string> skipFolders)
        {
            NamespaceUtility.CreateDotSettingsFile(path);

            var doc = new XmlDocument();
            doc.Load(path);

            foreach (string folder in skipFolders)
                NamespaceUtility.AddNamespaceFolderToSkip(doc, folder);

            NamespaceUtility.SaveDotSettings(doc, path);
        }
    }
}

#endif
