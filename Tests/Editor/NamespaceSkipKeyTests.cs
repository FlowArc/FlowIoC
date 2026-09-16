using System.Collections.Generic;
using System.Linq;
using FlowIoC.Editor.CodeStyle;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The key Rider looks a skipped folder up under. Get one character of the encoding wrong
    /// and the entry is silently ignored, so the shapes that matter are pinned: the backslash,
    /// the dot and the at sign a package cache folder carries, and the two spellings of a
    /// capital I once a Turkish machine has lowercased it.
    /// </summary>
    public class NamespaceSkipKeyTests
    {
        private const string Prefix = "/Default/CodeInspection/NamespaceProvider/NamespaceFoldersToSkip/=";
        private const string Suffix = "/@EntryIndexedValue";

        private static IReadOnlyList<string> For(string folder) => new NamespaceSkipKey().For(folder);

        [Test]
        public void A_cache_folder_is_lowercased_with_its_dots_at_sign_and_backslashes_encoded()
        {
            IReadOnlyList<string> keys = For(@"Library\PackageCache\com.flowarc.flowioc.core@b41df353fcba\Runtime");

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual(
                Prefix + "library_005Cpackagecache_005Ccom_002Eflowarc_002Eflowioc_002Ecore_0040b41df353fcba_005Cruntime" + Suffix,
                keys[0]);
        }

        [Test]
        public void A_forward_slash_reads_as_a_backslash()
        {
            Assert.AreEqual(For(@"Packages\Foo"), For("Packages/Foo"));
        }

        /// <summary>
        /// FlowIoC has a capital I, and a Turkish lowercase turns it into a dotless ı, which is
        /// what Rider writes on a Turkish machine and what it looks for there. Both spellings go
        /// in so that one file serves every machine.
        /// </summary>
        [Test]
        public void A_capital_I_is_spelt_both_ways()
        {
            IReadOnlyList<string> keys = For(@"Packages\FlowIoC\Editor");

            CollectionAssert.Contains(keys, Prefix + "packages_005Cflowioc_005Ceditor" + Suffix);
            CollectionAssert.Contains(keys, Prefix + "packages_005Cflow_0131oc_005Ceditor" + Suffix);
        }

        [Test]
        public void A_folder_without_a_capital_I_has_one_spelling()
        {
            Assert.AreEqual(1, For(@"Assets\Modules\PlayerModule\Scripts").Count);
        }

        [Test]
        public void A_hyphen_and_an_underscore_are_encoded_too()
        {
            string key = For(@"Packages\my-pkg_2").Single();

            StringAssert.Contains("packages_005Cmy_002Dpkg_005F2", key);
        }
    }
}
