#if UNITY_EDITOR
using System.IO;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.ModuleCards;
using FlowIoC.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The module a channel belongs to, found through the index. A channel is named for its
    /// module, so the name is the lookup; Default and a channel declared by hand belong to none.
    /// </summary>
    internal class ChannelModuleLocator
    {
        internal bool TryFolderOf(string channelName, out string absolutePath)
        {
            absolutePath = null;

            ED_ModuleIndex index = new ModuleIndexProvider().LoadOrCreate();
            if (index == null || !index.TryGetByName(channelName, out ModuleDescriptorEVO module)) return false;

            string assetPath = AssetDatabase.GUIDToAssetPath(module.FolderGuid);
            if (string.IsNullOrEmpty(assetPath)) return false;

            absolutePath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            return true;
        }
    }

    /// <summary>
    /// Edits a module channel's colour and profile - and writes them into the module's card, as
    /// the Colour and Profile lines above the generated block, rather than into any shared asset.
    /// The module owns its colour the way it owns its channel. Apply regenerates the module's
    /// part, so what the console shows follows on the next compile.
    ///
    /// A colour left on the palette's pick writes no Colour line, so a card says something only
    /// when somebody chose; a profile left on the default tag - "[Player]" in the module's colour
    /// - writes no Profile line for the same reason. A prefix cleared out writes
    /// <c>Profile: none</c>, because a card with no line would put the default tag back, and
    /// Reset takes both lines off the card. Cancel writes nothing.
    /// </summary>
    internal class FlowChannelStyleWindow : EditorWindow
    {
        private const float LabelWidth = 65f;
        private const float TextWidth = 150f;
        private const float StyleWidth = 90f;
        private const float ColourWidth = 40f;

        private readonly FlowChannelPalette _palette = new FlowChannelPalette();
        private readonly FlowLogProfileLine _profileLine = new FlowLogProfileLine();
        private readonly ModuleCardChannelLines _lines = new ModuleCardChannelLines();
        private readonly ModuleCardFile _cards = new ModuleCardFile();

        private string _channelName;
        private Color _colour;
        private string _prefix = "";
        private FlowTextStyle _prefixStyle;
        private Color _prefixColour = Color.white;
        private FlowTextStyle _messageStyle;
        private Color _messageColour = Color.white;
        private string _postfix = "";
        private FlowTextStyle _postfixStyle;
        private Color _postfixColour = Color.white;

        /// <summary>
        /// Whether the prefix is still the default tag, whose colour is the module's: then a new
        /// module colour carries the tag's colour with it, until the tag's own colour is touched.
        /// </summary>
        private bool _prefixFollowsColour;

        internal static void Open(FlowLogChannel channel)
        {
            var window = CreateInstance<FlowChannelStyleWindow>();
            window.titleContent = new GUIContent(channel.Name);
            window.Load(channel);
            window.minSize = new Vector2(400f, 230f);
            window.maxSize = new Vector2(400f, 230f);
            window.ShowUtility();
        }

        private void Load(FlowLogChannel channel)
        {
            _channelName = channel.Name;
            _colour = channel.Color;

            FlowLogProfile profile = channel.Profile;
            if (profile == null) return;

            _prefix = profile.Prefix ?? "";
            _prefixStyle = profile.PrefixStyle;
            _prefixColour = profile.PrefixColor;
            _messageStyle = profile.MessageStyle;
            _messageColour = profile.MessageColor;
            _postfix = profile.Postfix ?? "";
            _postfixStyle = profile.PostfixStyle;
            _postfixColour = profile.PostfixColor;

            _prefixFollowsColour = IsSame(profile, FlowLogChannels.DefaultProfileFor(channel.Name, channel.Color));
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Written into the module's MODULE.md, above the generated block. The console follows on the next compile.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Colour", GUILayout.Width(LabelWidth));
            _colour = EditorGUILayout.ColorField(GUIContent.none, _colour, true, false, false, GUILayout.Width(ColourWidth));
            GUILayout.Space(8f);

            if (GUILayout.Button("Palette", EditorStyles.miniButton, GUILayout.Width(60f)))
                _colour = _palette.Pick(_channelName);

            if (_prefixFollowsColour) _prefixColour = _colour;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(LabelWidth));
            EditorGUILayout.LabelField("Text", EditorStyles.miniLabel, GUILayout.Width(TextWidth));
            EditorGUILayout.LabelField("Style", EditorStyles.miniLabel, GUILayout.Width(StyleWidth));
            EditorGUILayout.LabelField("Colour", EditorStyles.miniLabel, GUILayout.Width(ColourWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefix", GUILayout.Width(LabelWidth));
            _prefix = EditorGUILayout.TextField(_prefix, GUILayout.Width(TextWidth));
            _prefixStyle = (FlowTextStyle) EditorGUILayout.EnumFlagsField(_prefixStyle, GUILayout.Width(StyleWidth));

            Color prefixColourBefore = _prefixColour;
            _prefixColour = EditorGUILayout.ColorField(GUIContent.none, _prefixColour, true, false, false, GUILayout.Width(ColourWidth));
            if (_prefixColour != prefixColourBefore) _prefixFollowsColour = false;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Message", GUILayout.Width(LabelWidth));
            EditorGUILayout.LabelField("", GUILayout.Width(TextWidth));
            _messageStyle = (FlowTextStyle) EditorGUILayout.EnumFlagsField(_messageStyle, GUILayout.Width(StyleWidth));
            _messageColour = EditorGUILayout.ColorField(GUIContent.none, _messageColour, true, false, false, GUILayout.Width(ColourWidth));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Postfix", GUILayout.Width(LabelWidth));
            _postfix = EditorGUILayout.TextField(_postfix, GUILayout.Width(TextWidth));
            _postfixStyle = (FlowTextStyle) EditorGUILayout.EnumFlagsField(_postfixStyle, GUILayout.Width(StyleWidth));
            _postfixColour = EditorGUILayout.ColorField(GUIContent.none, _postfixColour, true, false, false, GUILayout.Width(ColourWidth));
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Reset", "Back to the palette's colour and the default tag."),
                    GUILayout.Width(80f)))
            {
                Write(null, null);
                Close();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                Close();

            if (GUILayout.Button("Apply", GUILayout.Width(80f)))
            {
                Apply();
                Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f);
        }

        private void Apply()
        {
            Color32 chosen = _colour;
            Color32 pick = _palette.Pick(_channelName);
            bool onPalette = chosen.r == pick.r && chosen.g == pick.g && chosen.b == pick.b && chosen.a == pick.a;

            FlowLogProfile profile = BuildProfile();
            bool onDefault = IsSame(profile, FlowLogChannels.DefaultProfileFor(_channelName, _colour));

            Write(onPalette ? null : "#" + FlowLogProfileLine.HexOf(_colour),
                onDefault ? null : _profileLine.Format(profile));
        }

        /// <summary>The card with these two lines - null for neither - and the part regenerated from it.</summary>
        private void Write(string colourLine, string profileLine)
        {
            if (!new ChannelModuleLocator().TryFolderOf(_channelName, out string moduleFolder))
            {
                Debug.LogWarning($"<color=cyan>FlowConsole:</color> no module owns the {_channelName} channel, so there is no card to write.");
                return;
            }

            string card = _cards.Read(moduleFolder) ?? new ModuleCardStub().For(_channelName);

            _cards.Write(moduleFolder, _lines.Write(card, colourLine, profileLine));
            AssetDatabase.ImportAsset(AssetPathOf(_cards.PathFor(moduleFolder)));

            FlowModuleGenerator.Generate();
        }

        /// <summary>
        /// Whether two profiles decorate a line the same way. Colours are compared as the card
        /// writes them, to the byte, so a colour that went through the field and came back is
        /// still the same colour.
        /// </summary>
        private static bool IsSame(FlowLogProfile left, FlowLogProfile right)
        {
            if (left == null || right == null) return left == right;

            return left.Prefix == right.Prefix
                   && left.PrefixStyle == right.PrefixStyle
                   && FlowLogProfileLine.HexOf(left.PrefixColor) == FlowLogProfileLine.HexOf(right.PrefixColor)
                   && left.MessageStyle == right.MessageStyle
                   && FlowLogProfileLine.HexOf(left.MessageColor) == FlowLogProfileLine.HexOf(right.MessageColor)
                   && left.Postfix == right.Postfix
                   && left.PostfixStyle == right.PostfixStyle
                   && FlowLogProfileLine.HexOf(left.PostfixColor) == FlowLogProfileLine.HexOf(right.PostfixColor);
        }

        private FlowLogProfile BuildProfile()
        {
            var profile = new FlowLogProfile();

            if (!string.IsNullOrEmpty(_prefix))
                profile.SetPrefix(_prefix, _prefixStyle, _prefixColour);

            if (_messageStyle != FlowTextStyle.None)
                profile.SetMessageStyle(_messageStyle);

            if (_messageColour != Color.white)
                profile.SetMessageColor(_messageColour);

            if (!string.IsNullOrEmpty(_postfix))
                profile.SetPostfix(_postfix, _postfixStyle, _postfixColour);

            return profile;
        }

        private static string AssetPathOf(string absolutePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;

            return Path.GetRelativePath(projectRoot, absolutePath).Replace('\\', '/');
        }
    }
}
#endif
