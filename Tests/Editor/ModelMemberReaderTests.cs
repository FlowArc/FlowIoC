using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModelViewer;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What the Model Viewer shows of an object, and how it says it. The reader is the whole of the
    /// viewer's reflection - the window only draws what comes back - so every rule the Help page
    /// states about a member is a case here.
    /// </summary>
    public class ModelMemberReaderTests
    {
        private class Base
        {
            [ShowInModelViewer] private int _inherited = 7;

            public int ReadInherited() => _inherited;
        }

        private class Probe : Base
        {
            public int Count = 3;
            [HideInModelViewer] public int Secret = 9;
            [ShowInModelViewer] private float _shown = 1.5f;
            private float _unshown = 2.5f;
            public static int Static = 4;

            public string Name { get; set; } = "probe";
            public int GetOnly { get; } = 5;
            public int SetOnly { set { } }
            public int this[int index] => index;
            public int Throws => throw new InvalidOperationException("no");

            public float ReadUnshown() => _unshown;
        }

        private class Spoken
        {
            public override string ToString() => "spoken";
        }

        private class Silent
        {
        }

        private struct Pair
        {
            public int Left;
            public int Right;
        }

        private class ProbeAsset : ScriptableObject
        {
        }

        private readonly ModelMemberReader _reader = new ModelMemberReader();

        // ---- Members

        [Test]
        public void A_public_field_and_a_public_property_are_shown()
        {
            List<string> names = Names(new Probe());

            CollectionAssert.Contains(names, "Count");
            CollectionAssert.Contains(names, "Name");
            CollectionAssert.Contains(names, "GetOnly");
        }

        [Test]
        public void A_public_member_marked_hidden_is_not_shown()
        {
            CollectionAssert.DoesNotContain(Names(new Probe()), "Secret");
        }

        [Test]
        public void A_private_member_is_shown_only_when_marked()
        {
            List<string> names = Names(new Probe());

            CollectionAssert.Contains(names, "_shown");
            CollectionAssert.DoesNotContain(names, "_unshown");
        }

        [Test]
        public void A_static_member_an_indexer_a_setter_only_property_and_a_backing_field_are_not_shown()
        {
            List<string> names = Names(new Probe());

            CollectionAssert.DoesNotContain(names, "Static");
            CollectionAssert.DoesNotContain(names, "Item");
            CollectionAssert.DoesNotContain(names, "SetOnly");
            Assert.That(names.Any(name => name.Contains("<")), Is.False, "no compiler-generated member");
        }

        [Test]
        public void A_base_class_member_is_shown_after_the_derived_ones()
        {
            List<string> names = Names(new Probe());

            CollectionAssert.Contains(names, "_inherited");
            Assert.That(names.IndexOf("Count"), Is.LessThan(names.IndexOf("_inherited")));
        }

        [Test]
        public void A_getter_that_throws_is_a_fault_and_not_a_crash()
        {
            ModelNodeEVO node = _reader.Members(new Probe()).Single(member => member.Name == "Throws");

            Assert.That(node.Fault, Is.EqualTo("InvalidOperationException: no"));
            Assert.That(node.Value, Is.Null);
            Assert.That(node.Type, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void A_member_carries_its_value_and_declared_type()
        {
            ModelNodeEVO node = _reader.Members(new Probe()).Single(member => member.Name == "_shown");

            Assert.That(node.Value, Is.EqualTo(1.5f));
            Assert.That(node.Type, Is.EqualTo(typeof(float)));
            Assert.That(node.Fault, Is.Null);
        }

        // ---- Leaves

        [Test]
        public void Primitives_strings_enums_unity_structs_objects_types_and_delegates_are_leaves()
        {
            Assert.That(_reader.IsLeaf(typeof(int)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(string)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(FlowRole)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(Vector3)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(ScriptableObject)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(Type)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(Action)), Is.True);
            Assert.That(_reader.IsLeaf(typeof(int?)), Is.True);
        }

        [Test]
        public void Collections_classes_and_structs_are_not_leaves()
        {
            Assert.That(_reader.IsLeaf(typeof(List<int>)), Is.False);
            Assert.That(_reader.IsLeaf(typeof(Probe)), Is.False);
            Assert.That(_reader.IsLeaf(typeof(Pair)), Is.False);
        }

        [Test]
        public void An_empty_collection_does_not_expand_and_a_filled_one_does()
        {
            Assert.That(_reader.IsExpandable(new List<int>()), Is.False);
            Assert.That(_reader.IsExpandable(new List<int> {1}), Is.True);
            Assert.That(_reader.IsExpandable(new HashSet<int> {1}), Is.True);
            Assert.That(_reader.IsExpandable(new Dictionary<string, int>()), Is.False);
        }

        [Test]
        public void Null_a_string_and_a_leaf_do_not_expand_and_an_object_does()
        {
            Assert.That(_reader.IsExpandable(null), Is.False);
            Assert.That(_reader.IsExpandable("text"), Is.False);
            Assert.That(_reader.IsExpandable(3), Is.False);
            Assert.That(_reader.IsExpandable(new Probe()), Is.True);
            Assert.That(_reader.IsExpandable(new Pair()), Is.True);
        }

        // ---- Describe

        [Test]
        public void Null_is_the_word_null()
        {
            Assert.That(_reader.Describe(null, typeof(object)), Is.EqualTo("null"));
        }

        [Test]
        public void A_string_is_quoted_with_its_newlines_written_out()
        {
            Assert.That(_reader.Describe("a\nb", typeof(string)), Is.EqualTo("\"a\\nb\""));
        }

        [Test]
        public void A_long_string_is_cut_at_a_hundred_characters()
        {
            string text = new string('x', 120);

            string described = _reader.Describe(text, typeof(string));

            Assert.That(described, Is.EqualTo("\"" + new string('x', 99) + "…\""));
        }

        [Test]
        public void Primitives_use_the_invariant_culture_and_an_enum_its_name()
        {
            Assert.That(_reader.Describe(true, typeof(bool)), Is.EqualTo("true"));
            Assert.That(_reader.Describe(1.5f, typeof(float)), Is.EqualTo("1.5"));
            Assert.That(_reader.Describe(FlowRole.Service, typeof(FlowRole)), Is.EqualTo("Service"));
            Assert.That(_reader.Describe('c', typeof(char)), Is.EqualTo("'c'"));
        }

        [Test]
        public void A_type_is_its_friendly_name()
        {
            Assert.That(_reader.Describe(typeof(List<int>), typeof(Type)), Is.EqualTo("List<int>"));
        }

        [Test]
        public void A_collection_says_how_many_items_it_holds()
        {
            Assert.That(_reader.Describe(new List<int> {1}, typeof(List<int>)), Is.EqualTo("1 item"));
            Assert.That(_reader.Describe(new List<int> {1, 2}, typeof(List<int>)), Is.EqualTo("2 items"));
            Assert.That(_reader.Describe(new HashSet<int> {1, 2, 3}, typeof(HashSet<int>)), Is.EqualTo("3 items"));
        }

        [Test]
        public void A_tuple_describes_each_item()
        {
            Assert.That(_reader.Describe((0, typeof(int)), typeof((int, Type))), Is.EqualTo("(0, int)"));
        }

        [Test]
        public void A_delegate_says_how_many_targets_it_has()
        {
            Action handler = () => { };
            handler += () => { };

            Assert.That(_reader.Describe(handler, typeof(Action)), Is.EqualTo("Action · 2 targets"));
        }

        [Test]
        public void An_object_that_overrides_ToString_is_described_by_it()
        {
            Assert.That(_reader.Describe(new Spoken(), typeof(Spoken)), Is.EqualTo("spoken"));
        }

        [Test]
        public void An_object_without_ToString_names_its_type_only_when_it_differs_from_the_declared_one()
        {
            Assert.That(_reader.Describe(new Silent(), typeof(Silent)), Is.EqualTo(""));
            Assert.That(_reader.Describe(new Silent(), typeof(object)), Is.EqualTo("Silent"));
            Assert.That(_reader.Describe(new Silent(), null), Is.EqualTo("Silent"));
        }

        [Test]
        public void A_unity_object_is_its_name_and_type_and_a_destroyed_one_says_so()
        {
            ProbeAsset asset = ScriptableObject.CreateInstance<ProbeAsset>();
            asset.name = "Asset";

            Assert.That(_reader.Describe(asset, typeof(ProbeAsset)), Is.EqualTo("Asset (ProbeAsset)"));

            Object.DestroyImmediate(asset);

            Assert.That(_reader.Describe(asset, typeof(ProbeAsset)), Is.EqualTo("null (destroyed)"));
        }

        // ---- Children

        [Test]
        public void A_dictionary_child_is_named_by_its_key_and_typed_by_the_value_type()
        {
            var dictionary = new Dictionary<string, int> {{"a", 1}, {"b", 2}};

            List<ModelNodeEVO> children = _reader.Children(dictionary, typeof(Dictionary<string, int>), ModelMemberReader.PAGE, out int hidden);

            Assert.That(hidden, Is.EqualTo(0));
            CollectionAssert.AreEqual(new[] {"a", "b"}, children.Select(child => child.Name));
            CollectionAssert.AreEqual(new object[] {1, 2}, children.Select(child => child.Value));
            Assert.That(children.All(child => child.Type == typeof(int)), Is.True);
        }

        [Test]
        public void A_dictionary_key_that_is_not_a_string_is_described()
        {
            var dictionary = new Dictionary<(int, Type), string> {{(0, typeof(int)), "x"}};

            List<ModelNodeEVO> children = _reader.Children(dictionary, typeof(Dictionary<(int, Type), string>), ModelMemberReader.PAGE, out _);

            Assert.That(children.Single().Name, Is.EqualTo("(0, int)"));
        }

        [Test]
        public void A_list_child_is_named_by_its_index_and_typed_by_the_element_type()
        {
            List<ModelNodeEVO> children = _reader.Children(new List<string> {"x", "y"}, typeof(List<string>), ModelMemberReader.PAGE, out _);

            CollectionAssert.AreEqual(new[] {"[0]", "[1]"}, children.Select(child => child.Name));
            Assert.That(children.All(child => child.Type == typeof(string)), Is.True);
        }

        [Test]
        public void A_set_and_an_array_are_walked_like_a_list()
        {
            List<ModelNodeEVO> set = _reader.Children(new HashSet<int> {5}, typeof(HashSet<int>), ModelMemberReader.PAGE, out _);
            List<ModelNodeEVO> array = _reader.Children(new[] {1.5f}, typeof(float[]), ModelMemberReader.PAGE, out _);

            Assert.That(set.Single().Name, Is.EqualTo("[0]"));
            Assert.That(set.Single().Type, Is.EqualTo(typeof(int)));
            Assert.That(array.Single().Type, Is.EqualTo(typeof(float)));
        }

        [Test]
        public void Children_past_the_limit_are_counted_and_not_returned()
        {
            List<int> list = Enumerable.Range(0, 60).ToList();

            List<ModelNodeEVO> children = _reader.Children(list, typeof(List<int>), ModelMemberReader.PAGE, out int hidden);

            Assert.That(children.Count, Is.EqualTo(ModelMemberReader.PAGE));
            Assert.That(hidden, Is.EqualTo(10));
            Assert.That(children.Last().Name, Is.EqualTo("[49]"));
        }

        [Test]
        public void An_object_s_children_are_its_members()
        {
            List<ModelNodeEVO> children = _reader.Children(new Pair {Left = 1, Right = 2}, typeof(Pair), ModelMemberReader.PAGE, out _);

            CollectionAssert.AreEquivalent(new[] {"Left", "Right"}, children.Select(child => child.Name));
        }

        [Test]
        public void A_string_has_no_children()
        {
            Assert.That(_reader.Children("text", typeof(string), ModelMemberReader.PAGE, out _), Is.Empty);
        }

        // ---- Type names

        [Test]
        public void Type_names_are_short_and_read_like_source()
        {
            Assert.That(_reader.TypeName(typeof(int)), Is.EqualTo("int"));
            Assert.That(_reader.TypeName(typeof(string)), Is.EqualTo("string"));
            Assert.That(_reader.TypeName(typeof(int?)), Is.EqualTo("int?"));
            Assert.That(_reader.TypeName(typeof(int[])), Is.EqualTo("int[]"));
            Assert.That(_reader.TypeName(typeof(Dictionary<int, List<string>>)), Is.EqualTo("Dictionary<int, List<string>>"));
            Assert.That(_reader.TypeName(typeof((int, Type))), Is.EqualTo("(int, Type)"));
            Assert.That(_reader.TypeName(typeof(Probe)), Is.EqualTo("Probe"));
            Assert.That(_reader.TypeName(null), Is.EqualTo(""));
        }

        private List<string> Names(object owner) => _reader.Members(owner).Select(member => member.Name).ToList();
    }
}
