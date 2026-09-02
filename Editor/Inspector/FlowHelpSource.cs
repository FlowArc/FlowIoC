#if UNITY_EDITOR

using System;
using System.Collections.Generic;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The help text a member carries, wherever in the type's ancestry it was declared. A Root's
    /// inspector asks for AutoSetup and gets what RootBase documents, because that is where the
    /// field lives.
    ///
    /// Each type's file is read and parsed once and kept for the life of this instance. The
    /// instance belongs to an editor, and a comment cannot change without a recompile, which
    /// builds a new one.
    /// </summary>
    internal class FlowHelpSource
    {
        private readonly IFlowScriptText _texts;
        private readonly FlowHelpParser _parser = new FlowHelpParser();

        private readonly Dictionary<Type, Dictionary<string, string>> _cache =
            new Dictionary<Type, Dictionary<string, string>>();

        public FlowHelpSource(IFlowScriptText texts)
        {
            _texts = texts;
        }

        public string For(Type type, string member)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (HelpFor(current).TryGetValue(member, out string text))
                    return text;
            }

            return null;
        }

        public string Summary(Type type)
        {
            return HelpFor(type).TryGetValue(FlowHelpParser.TypeKey, out string text) ? text : null;
        }

        private Dictionary<string, string> HelpFor(Type type)
        {
            if (_cache.TryGetValue(type, out Dictionary<string, string> help))
                return help;

            help = _parser.Parse(_texts.Read(type));
            _cache[type] = help;

            return help;
        }
    }
}

#endif
