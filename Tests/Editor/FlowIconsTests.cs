using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowIoC.Editor.Icons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The icons are drawn at the pixels they ship at and never resampled, which holds only while
    /// every icon ships at every size. These tests are what keeps a new FlowIcon value from being
    /// added without its files, and a stray file from sitting in the folder with no value to draw
    /// it.
    /// </summary>
    public class FlowIconsTests
    {
        private static IEnumerable<FlowIcon> ShippedIcons() =>
            Enum.GetValues(typeof(FlowIcon)).Cast<FlowIcon>().Where(icon => icon != FlowIcon.None);

        [Test]
        public void Every_icon_ships_at_every_size()
        {
            var missing = new List<string>();

            foreach (FlowIcon icon in ShippedIcons())
            {
                foreach (int size in FlowIcons.ShippedSizes)
                {
                    string path = $"{FlowIcons.Folder()}/{FlowIcons.FileName(icon, size)}";

                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                        missing.Add(path);
                }
            }

            Assert.IsEmpty(missing, "Missing:\n" + string.Join("\n", missing));
        }

        /// <summary>
        /// A file is only as crisp as its pixels match the draw, so a 16 that is really a 17
        /// would be resampled after all.
        /// </summary>
        [Test]
        public void Every_file_is_the_size_its_name_says()
        {
            var wrong = new List<string>();

            foreach (FlowIcon icon in ShippedIcons())
            {
                foreach (int size in FlowIcons.ShippedSizes)
                {
                    string path = $"{FlowIcons.Folder()}/{FlowIcons.FileName(icon, size)}";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    if (texture != null && (texture.width != size || texture.height != size))
                        wrong.Add($"{path} is {texture.width}x{texture.height}");
                }
            }

            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// The importer is what keeps a drawing crisp after it is rasterised: a mip chain or a
        /// compressed format would resample it after all. The importer is applied by
        /// FlowIconImporter, and this is what proves it ran over every file.
        /// </summary>
        [Test]
        public void Every_file_is_imported_as_a_plain_gui_texture()
        {
            var wrong = new List<string>();

            foreach (FlowIcon icon in ShippedIcons())
            {
                foreach (int size in FlowIcons.ShippedSizes)
                {
                    string path = $"{FlowIcons.Folder()}/{FlowIcons.FileName(icon, size)}";
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                    if (importer == null)
                        continue;

                    if (importer.textureType != TextureImporterType.GUI || importer.mipmapEnabled
                                                                        || importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        wrong.Add(path);
                    }
                }
            }

            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        [Test]
        public void Every_file_in_the_folder_belongs_to_an_icon()
        {
            var expected = new HashSet<string>();

            foreach (FlowIcon icon in ShippedIcons())
            {
                foreach (int size in FlowIcons.ShippedSizes)
                    expected.Add(FlowIcons.FileName(icon, size));
            }

            string[] strays = Directory.GetFiles(FlowIcons.Folder(), "*.png")
                .Select(Path.GetFileName)
                .Where(name => !expected.Contains(name))
                .ToArray();

            Assert.IsEmpty(strays, string.Join("\n", strays));
        }

        /// <summary>
        /// Every value carries its number so that a value added or removed leaves the others where
        /// they were; a duplicate would make two names one picture.
        /// </summary>
        [Test]
        public void No_two_icons_share_a_number()
        {
            int[] numbers = Enum.GetValues(typeof(FlowIcon)).Cast<int>().ToArray();

            Assert.AreEqual(numbers.Length, numbers.Distinct().Count());
        }

        [Test]
        public void A_draw_asks_for_the_shipped_size_that_matches_its_pixels()
        {
            // Shipped sizes stand in for the two point sizes at 1x and at 2x.
            Assert.AreEqual(16, FlowIcons.PixelsFor(16f / EditorGUIUtility.pixelsPerPoint));
            Assert.AreEqual(24, FlowIcons.PixelsFor(24f / EditorGUIUtility.pixelsPerPoint));
            Assert.AreEqual(32, FlowIcons.PixelsFor(32f / EditorGUIUtility.pixelsPerPoint));
            Assert.AreEqual(48, FlowIcons.PixelsFor(48f / EditorGUIUtility.pixelsPerPoint));
        }

        [Test]
        public void A_draw_between_two_shipped_sizes_takes_the_one_above()
        {
            Assert.AreEqual(24, FlowIcons.PixelsFor(20f / EditorGUIUtility.pixelsPerPoint));
            Assert.AreEqual(48, FlowIcons.PixelsFor(40f / EditorGUIUtility.pixelsPerPoint));
            Assert.AreEqual(48, FlowIcons.PixelsFor(64f / EditorGUIUtility.pixelsPerPoint));
        }
    }
}