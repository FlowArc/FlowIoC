using System;
using System.Collections.Generic;
using FlowIoC.Editor.Root;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The resolver turns a context type into the script asset that declares it, which is what a
    /// Root's sub-context entry stores from now on. A name search alone is not enough: two modules
    /// may each hold a class called SettingsScreenContext, and the entry has to point at the right
    /// one - so the candidates are filtered on the class the script actually compiles to.
    /// </summary>
    public class ContextScriptResolverTests
    {
        private class Player
        {
        }

        private class Other
        {
        }

        /// <summary>
        /// A stand-in for a MonoScript. The resolver never asks what kind of Object it holds - the
        /// class behind it comes through the seam - so a ScriptableObject is enough to carry
        /// identity, and a test needs no asset on disk.
        /// </summary>
        private static Object Script() => ScriptableObject.CreateInstance<ScriptableObject>();

        private static ContextScriptResolver Resolver(
            Dictionary<string, IReadOnlyList<Object>> byName,
            Dictionary<Object, Type> classes,
            params Type[] contextTypes)
        {
            return new ContextScriptResolver(
                name => byName.TryGetValue(name, out IReadOnlyList<Object> found)
                    ? found
                    : new List<Object>(),
                script => classes.TryGetValue(script, out Type type) ? type : null,
                () => contextTypes);
        }

        [Test]
        public void A_type_resolves_to_the_script_that_declares_it()
        {
            Object script = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>> {{"Player", new List<Object> {script}}},
                new Dictionary<Object, Type> {{script, typeof(Player)}});

            Assert.AreSame(script, resolver.For(typeof(Player)));
        }

        /// <summary>
        /// The failure this exists for: FindAssets matches on the file name, so a class of the same
        /// name in another module comes back among the candidates. Taking the first would wire the
        /// Root to the wrong module's context, and nothing downstream could tell.
        /// </summary>
        [Test]
        public void A_same_named_class_from_elsewhere_is_not_mistaken_for_it()
        {
            Object other = Script();
            Object wanted = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>> {{"Player", new List<Object> {other, wanted}}},
                new Dictionary<Object, Type> {{other, typeof(Other)}, {wanted, typeof(Player)}});

            Assert.AreSame(wanted, resolver.For(typeof(Player)));
        }

        /// <summary>
        /// A type from a precompiled assembly has no script asset, and so does a class whose file
        /// is named something else - MonoScript.GetClass answers null there. Both are ordinary
        /// rather than errors, and the entry keeps the name it already had.
        /// </summary>
        [Test]
        public void A_type_with_no_script_asset_resolves_to_null()
        {
            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>>(),
                new Dictionary<Object, Type>());

            Assert.IsNull(resolver.For(typeof(Player)));
        }

        [Test]
        public void A_candidate_whose_class_cannot_be_read_is_passed_over()
        {
            Object unreadable = Script();
            Object wanted = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>> {{"Player", new List<Object> {unreadable, wanted}}},
                new Dictionary<Object, Type> {{wanted, typeof(Player)}});

            Assert.AreSame(wanted, resolver.For(typeof(Player)));
        }

        [Test]
        public void A_null_type_resolves_to_null()
        {
            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>>(),
                new Dictionary<Object, Type>());

            Assert.IsNull(resolver.For(null));
        }

        [Test]
        public void The_class_behind_a_script_is_read_back()
        {
            Object script = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>>(),
                new Dictionary<Object, Type> {{script, typeof(Player)}});

            Assert.AreEqual(typeof(Player), resolver.TypeOf(script));
        }

        [Test]
        public void A_null_script_has_no_class_behind_it()
        {
            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>>(),
                new Dictionary<Object, Type>());

            Assert.IsNull(resolver.TypeOf(null));
        }

        /// <summary>
        /// The name is what an entry written before the script reference carried, and what the
        /// Root inspector's Resolve button has to work from. It goes through the type rather than
        /// straight to a file, so the answer is the class the project actually compiled.
        /// </summary>
        [Test]
        public void A_full_name_resolves_to_the_script_of_the_type_it_names()
        {
            Object script = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>> {{"Player", new List<Object> {script}}},
                new Dictionary<Object, Type> {{script, typeof(Player)}},
                typeof(Player));

            Assert.AreSame(script, resolver.ForName(typeof(Player).FullName));
        }

        /// <summary>
        /// A name left behind by a module that was deleted. Nothing compiles to it, so there is no
        /// script to point at, and the entry stays as it is for the inspector to report.
        /// </summary>
        [Test]
        public void A_full_name_no_type_answers_to_resolves_to_null()
        {
            Object script = Script();

            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>> {{"Player", new List<Object> {script}}},
                new Dictionary<Object, Type> {{script, typeof(Player)}},
                typeof(Player));

            Assert.IsNull(resolver.ForName("Modules.Gone.RootsContexts.GoneContext"));
        }

        [Test]
        public void An_empty_full_name_resolves_to_null()
        {
            ContextScriptResolver resolver = Resolver(
                new Dictionary<string, IReadOnlyList<Object>>(),
                new Dictionary<Object, Type>());

            Assert.IsNull(resolver.ForName(string.Empty));
        }
    }
}