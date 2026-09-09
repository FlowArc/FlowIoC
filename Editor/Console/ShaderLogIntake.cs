#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace FlowIoC.Editor.Console
{
    /// <summary>
    /// The decisions the shader bridge makes about what it has been handed, kept apart from the
    /// import callback so they can be tested without an import to fire. Nothing here touches
    /// ShaderUtil: it is given the values already read off a message, so a test can hand it any
    /// shape it likes.
    /// </summary>
    public class ShaderLogIntake
    {
        /// <summary>
        /// The last set of messages reported for each asset path, so a reimport that changes
        /// nothing is not reported twice. Unity reimports a shader whenever a file it includes is
        /// touched, and a project with one broken shader would otherwise fill the console with the
        /// same two rows every time anybody saved an .hlsl.
        ///
        /// It is dropped by a domain reload, and so is FlowLogger.Logs - the two die together, so
        /// there is never a signature remembered for a row no longer on screen.
        /// </summary>
        private readonly Dictionary<string, string> _reported = new();

        /// <summary>
        /// The compiler's own words for every error already put on the Shader channel, which is
        /// what <see cref="WasReported"/> matches Unity's console text against.
        ///
        /// Shader errors arrive twice, the way compiler errors do. This bridge reads them off the
        /// asset at import, carrying the file and the line; Unity writes the same error to its own
        /// console later, when the variant actually compiles, as text carrying neither - so the
        /// second copy is a row that cannot be double-clicked anywhere. The second copy is the one
        /// dropped, and only when the first was actually recorded: a variant that fails at play
        /// time was never imported, so nothing here knows of it and Unity's copy is all there is.
        /// </summary>
        private readonly HashSet<string> _reportedTexts = new();

        /// <summary>
        /// Whether the path is an asset that can carry shader messages. Include files are not
        /// listed: an .hlsl holds no messages of its own, and editing one reimports every shader
        /// that includes it, which is where the error actually surfaces.
        /// </summary>
        public bool IsShaderAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;

            return EndsWith(assetPath, ".shader")
                   || EndsWith(assetPath, ".shadergraph")
                   || EndsWith(assetPath, ".compute")
                   || EndsWith(assetPath, ".raytrace");
        }

        /// <summary>
        /// Whether this set of messages is new for the path. Asking also records the answer, so a
        /// second call with the same set says no - which is what the caller wants and why this is
        /// not a plain query.
        /// </summary>
        public bool ShouldReport(string assetPath, IReadOnlyList<string> messages)
        {
            string signature = Signature(messages);

            if (signature.Length == 0)
            {
                // The shader compiles now. Forgotten rather than remembered as empty, so that
                // breaking it again later reports the same errors instead of being called a repeat.
                _reported.Remove(assetPath);
                return false;
            }

            if (_reported.TryGetValue(assetPath, out string previous) && previous == signature)
                return false;

            _reported[assetPath] = signature;
            return true;
        }

        /// <summary>
        /// Dropped when an asset is deleted or moved, so a path reused later starts clean.
        /// </summary>
        public void Forget(string assetPath)
        {
            _reported.Remove(assetPath);
        }

        /// <summary>
        /// Remembers the compiler's own words for an error about to be put on the Shader channel,
        /// so Unity's later copy of it can be recognised and dropped.
        /// </summary>
        public void Remember(string message)
        {
            if (!string.IsNullOrEmpty(message))
                _reportedTexts.Add(message);
        }

        /// <summary>
        /// Whether Unity's console text is one this bridge has already reported. Matched by
        /// containment, because Unity wraps the compiler's words in its own sentence - "Shader
        /// error in 'Name': &lt;words&gt; at Path(11) (on d3d11)" - and the words in the middle are
        /// the only part the two copies share.
        /// </summary>
        public bool WasReported(string unityConsoleText)
        {
            if (string.IsNullOrEmpty(unityConsoleText)) return false;

            foreach (string reported in _reportedTexts)
                if (unityConsoleText.Contains(reported))
                    return true;

            return false;
        }

        /// <summary>
        /// What the console row says. The channel's tag already reads "[Shader]", so the words
        /// "shader error" are not repeated here; the shader's own name is, because the path in the
        /// row's source is the file and a .shadergraph can hold a name that is not obvious from it.
        /// The platform is named because the same shader can compile on one and not another, and a
        /// reader who cannot see which would think the error was universal.
        ///
        /// The message alone, never messageDetails: Unity puts the subshader, the pass and the
        /// whole platform-defines dump in there, which is several hundred characters of UNITY_*
        /// keywords and would leave every shader row unreadable. That belongs in the detail pane,
        /// and <see cref="Detail"/> is what puts it there.
        /// </summary>
        public string Describe(string shaderName, string message, string platform)
        {
            var text = new StringBuilder();

            if (!string.IsNullOrEmpty(shaderName))
                text.Append(shaderName).Append(" - ");

            text.Append(string.IsNullOrEmpty(message) ? "compilation failed" : message);

            if (!string.IsNullOrEmpty(platform))
                text.Append(" (").Append(platform).Append(')');

            return text.ToString();
        }

        /// <summary>
        /// What the row expands into, which is where Unity's own console puts the same text. Sent
        /// as the log's trace: a shader error has no stack, and the keywords a variant was compiled
        /// with are what a reader opens the row to find out.
        /// </summary>
        public string Detail(string messageDetails)
        {
            return string.IsNullOrEmpty(messageDetails) ? null : messageDetails;
        }

        private static string Signature(IReadOnlyList<string> messages)
        {
            if (messages == null || messages.Count == 0) return "";

            var text = new StringBuilder();

            for (int i = 0; i < messages.Count; i++)
                text.Append(messages[i]).Append('\n');

            return text.ToString();
        }

        private static bool EndsWith(string value, string suffix)
        {
            return value.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif