#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FlowIoC.Editor.Help
{
    /// <summary>
    /// Turns a snippet into the rich text a code block is drawn from. The colours are Rider's own,
    /// so a reader who has the file open in the IDE sees the same snippet in the same colours in
    /// both places.
    ///
    /// This is a lexer and not a parser: the snippets are handwritten and a few lines long, so the
    /// shape of a token is enough to colour it and nothing here needs to know what compiles. What
    /// a name is comes from what sits either side of it - a name after a dot is a member, a name
    /// after another name is what is being declared, a name before a bracket is a call - and the
    /// handful of places that reasoning is wrong are places a doc snippet rarely goes.
    ///
    /// Every snippet is lexed once and kept, because a page rebuilds its strings on every repaint
    /// and the same handful of snippets would otherwise be lexed sixty times a second.
    /// </summary>
    internal class HelpCodeHighlighter
    {
        /// <summary>
        /// What is drawn as a keyword. The contextual ones are in here as well, because Rider
        /// colours them too and a snippet's `get; set;` is the reason to have them - but `value`,
        /// `add` and `remove` are left out, since outside the member that gives them meaning they
        /// are ordinary names and colouring one would be a lie.
        /// </summary>
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while",
            "async", "await", "get", "global", "init", "nameof", "partial", "record", "set", "var",
            "when", "where", "yield"
        };

        /// <summary>
        /// The keywords that are also a type, which is the one thing the lexer needs to tell them
        /// apart for: what follows one of these is a name being declared, so the `Currency` in
        /// `double Currency` is a property and not a class.
        /// </summary>
        private static readonly HashSet<string> TypeKeywords = new HashSet<string>
        {
            "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long",
            "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "var", "void"
        };

        /// <summary>
        /// The names on Context that read like types and are not. `CommandBinder.Bind(...)` looks
        /// exactly like a static call on a class, but the binder is a property the Context carries
        /// - which is why Rider draws it as a member - and no amount of looking at the characters
        /// either side would tell the two apart. Every snippet in the help window is FlowIoC's own,
        /// so the handful of names is worth naming.
        /// </summary>
        private static readonly HashSet<string> ContextMembers = new HashSet<string>
        {
            "AllContexts", "CommandBinder", "InjectionBinder", "InjectionBinderCrossContext",
            "MediationBinder", "SubContexts"
        };

        private readonly Dictionary<string, string> _highlighted = new Dictionary<string, string>();

        private readonly string _keyword;
        private readonly string _string;
        private readonly string _number;
        private readonly string _comment;
        private readonly string _method;
        private readonly string _type;
        private readonly string _field;

        public HelpCodeHighlighter(HelpTheme theme)
        {
            _keyword = Open(theme.CodeKeyword);
            _string = Open(theme.CodeString);
            _number = Open(theme.CodeNumber);
            _comment = Open(theme.CodeComment);
            _method = Open(theme.CodeMethod);
            _type = Open(theme.CodeType);
            _field = Open(theme.CodeField);
        }

        /// <summary>
        /// The snippet as rich text. What is not one of the coloured kinds is left alone and takes
        /// the code style's own colour, so punctuation and a plain identifier read as body.
        /// </summary>
        public string Highlight(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            if (_highlighted.TryGetValue(code, out string cached))
                return cached;

            string result = Build(code);
            _highlighted[code] = result;

            return result;
        }

        private string Build(string code)
        {
            var builder = new StringBuilder(code.Length + 128);
            int index = 0;

            // A preprocessor line is only a preprocessor line when the # is the first thing on it.
            bool atLineStart = true;

            // What came before the name being looked at, which is all the lexer has to go on. The
            // sign is the last character that was not whitespace, the word is the last name or
            // keyword, and afterType says the token before could have ended a type - a name, a
            // closing angle or square bracket, or one of the keywords that is itself a type.
            char sign = '\0';
            string word = null;
            bool afterType = false;

            // How many generic argument lists are open. A name between angle brackets is a type
            // argument however it is punctuated, so the rules that read a comma as an argument
            // separator have to know they are not in a call.
            int generic = 0;

            while (index < code.Length)
            {
                char current = code[index];

                if (current == '\n')
                {
                    builder.Append(current);
                    index++;
                    atLineStart = true;

                    continue;
                }

                if (char.IsWhiteSpace(current))
                {
                    builder.Append(current);
                    index++;

                    continue;
                }

                bool lineStart = atLineStart;
                atLineStart = false;

                if (lineStart && current == '#')
                {
                    // The directive is the keyword and the rest of the line is ordinary text: a
                    // compilation symbol is not a type, however much UNITY_EDITOR looks like one.
                    int directive = EndOfIdentifier(code, index);
                    index = Colour(builder, code, index, directive, _keyword);
                    index = Reset(Plain(builder, code, index, EndOfLine(code, index)),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (current == '/' && Next(code, index) == '/')
                {
                    index = Reset(Colour(builder, code, index, EndOfLine(code, index), _comment),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (current == '/' && Next(code, index) == '*')
                {
                    index = Reset(
                        Colour(builder, code, index, EndOfBlockComment(code, index), _comment),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (IsStringStart(code, index))
                {
                    index = Reset(Colour(builder, code, index, EndOfString(code, index), _string),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (current == '\'')
                {
                    index = Reset(Colour(builder, code, index, EndOfChar(code, index), _string),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (char.IsDigit(current))
                {
                    index = Reset(Colour(builder, code, index, EndOfNumber(code, index), _number),
                        ref sign, ref word, ref afterType);

                    continue;
                }

                if (IsIdentifierStart(current))
                {
                    int end = EndOfIdentifier(code, index);
                    string name = code.Substring(index, end - index);

                    index = Colour(builder, code, index, end,
                        Kind(code, name, index, end, sign, word, afterType, generic));

                    afterType = !Keywords.Contains(name) || TypeKeywords.Contains(name);
                    word = name;
                    sign = code[end - 1];

                    continue;
                }

                builder.Append(current);
                index++;

                if (current == '<' && afterType)
                    generic++;
                else if (current == '>' && generic > 0)
                    generic--;

                sign = current;
                afterType = current == '>' || current == ']';

                // The name before an opening bracket is kept, because it is what says whether the
                // argument inside is a type - `typeof(ViewInjector)` - or a method being handed on.
                if (current != '(')
                    word = null;
            }

            return builder.ToString();
        }

        /// <summary>
        /// What colour a name takes. The order is the order the answers are certain in: a keyword
        /// is one whatever surrounds it, a name Rider was told to treat as a type by `new` is a
        /// type, a name before a bracket is a call, and a name after a type is what that type is
        /// declaring. Only then does the shape of the name itself get a say.
        /// </summary>
        private string Kind(string code, string name, int start, int end, char sign, string word,
            bool afterType, int generic)
        {
            if (Keywords.Contains(name))
                return _keyword;

            bool afterDot = sign == '.';

            // An attribute is a class wearing brackets, so `[RequireComponent(...)]` is not the
            // call its parentheses make it look like.
            if (sign == '[')
                return _type;

            if (word == "new" && !afterDot)
                return _type;

            if (IsCall(code, end))
                return _method;

            if (ContextMembers.Contains(name))
                return _field;

            // `Signal<double> AddCurrency` and `IPlayerModel _playerModel`: a name that follows a
            // type is the member being declared, whatever case it is written in.
            if (afterType && !afterDot)
                return _field;

            if (name[0] == '_' || afterDot)
                return _field;

            if (!char.IsUpper(name[0]))
                return null;

            return IsMethodGroup(code, name, start, end, sign, word, generic) ? _method : _type;
        }

        /// <summary>
        /// Whether a name that is not being called is a method being handed to something else -
        /// `AddListener(OnCurrencyChanged)` and `_view.Play += PlayClicked`, which Rider draws as
        /// methods and not as the types their capital letter would otherwise make them.
        ///
        /// An argument on its own is one, unless the call it sits in is an operator that takes a
        /// type. A type argument is not, which is what the generic depth is here to rule out.
        /// </summary>
        private static bool IsMethodGroup(string code, string name, int start, int end, char sign,
            string word, int generic)
        {
            if (AfterEventAssignment(code, start))
                return true;

            if (generic > 0 || sign != '(' && sign != ',')
                return false;

            if (word == "typeof" || word == "default" || word == "sizeof")
                return false;

            char next = NextSign(code, end);

            return next == ')' || next == ',';
        }

        /// <summary>Whether the name is what a `+=` or a `-=` is handing an event.</summary>
        private static bool AfterEventAssignment(string code, int start)
        {
            int scan = start - 1;

            while (scan >= 0 && char.IsWhiteSpace(code[scan]))
                scan--;

            if (scan < 1 || code[scan] != '=')
                return false;

            return code[scan - 1] == '+' || code[scan - 1] == '-';
        }

        private static char NextSign(string code, int index)
        {
            while (index < code.Length && char.IsWhiteSpace(code[index]))
                index++;

            return index < code.Length ? code[index] : '\0';
        }

        /// <summary>
        /// Forgets what came before, which is what a comment, a literal or a number leaves behind:
        /// none of them can begin a type, and none of them is a name the next token can lean on.
        /// </summary>
        private static int Reset(int index, ref char sign, ref string word, ref bool afterType)
        {
            sign = '\0';
            word = null;
            afterType = false;

            return index;
        }

        /// <summary>
        /// Whether the name that ends at <paramref name="index"/> is being called. A generic call
        /// is one too, so a `&lt;...&gt;` between the name and its bracket is stepped over - and
        /// only over what an argument list may hold, so a `&lt;` that is really a comparison stops
        /// the scan rather than running to the end of the snippet.
        /// </summary>
        private static bool IsCall(string code, int index)
        {
            int scan = SkipSpaces(code, index);

            if (scan < code.Length && code[scan] == '<')
            {
                scan++;

                while (scan < code.Length && code[scan] != '>')
                {
                    char inside = code[scan];

                    if (!char.IsLetterOrDigit(inside) && inside != '_' && inside != ',' &&
                        inside != ' ' && inside != '.' && inside != '[' && inside != ']')
                    {
                        return false;
                    }

                    scan++;
                }

                if (scan >= code.Length)
                    return false;

                scan = SkipSpaces(code, scan + 1);
            }

            return scan < code.Length && code[scan] == '(';
        }

        private static int SkipSpaces(string code, int index)
        {
            while (index < code.Length && (code[index] == ' ' || code[index] == '\t'))
                index++;

            return index;
        }

        /// <summary>An ordinary string, a verbatim one, an interpolated one, or both at once.</summary>
        private static bool IsStringStart(string code, int index)
        {
            char current = code[index];

            if (current == '"')
                return true;

            if (current != '@' && current != '$')
                return false;

            char next = Next(code, index);

            if (next == '"')
                return true;

            return (next == '@' || next == '$') && index + 2 < code.Length && code[index + 2] == '"';
        }

        private static int EndOfString(string code, int index)
        {
            bool verbatim = false;
            int scan = index;

            while (scan < code.Length && code[scan] != '"')
            {
                verbatim |= code[scan] == '@';
                scan++;
            }

            scan++;

            while (scan < code.Length)
            {
                char current = code[scan];

                if (verbatim)
                {
                    // A doubled quote inside a verbatim string is one quote, not the end of it.
                    if (current == '"' && Next(code, scan) != '"')
                        return scan + 1;

                    scan += current == '"' ? 2 : 1;

                    continue;
                }

                if (current == '\\')
                {
                    scan += 2;

                    continue;
                }

                if (current == '"')
                    return scan + 1;

                // An unterminated string is a snippet that was cut mid-line. It ends where the
                // line does rather than swallowing the rest of the block.
                if (current == '\n')
                    return scan;

                scan++;
            }

            return scan;
        }

        private static int EndOfChar(string code, int index)
        {
            int scan = index + 1;

            while (scan < code.Length && code[scan] != '\n')
            {
                if (code[scan] == '\\')
                {
                    scan += 2;

                    continue;
                }

                if (code[scan] == '\'')
                    return scan + 1;

                scan++;
            }

            return scan;
        }

        private static int EndOfNumber(string code, int index)
        {
            int scan = index;

            while (scan < code.Length)
            {
                char current = code[scan];

                bool part = char.IsLetterOrDigit(current) || current == '_' ||
                            (current == '.' && scan + 1 < code.Length && char.IsDigit(code[scan + 1]));

                if (!part)
                    break;

                scan++;
            }

            return scan;
        }

        private static int EndOfIdentifier(string code, int index)
        {
            int scan = index + 1;

            while (scan < code.Length && (char.IsLetterOrDigit(code[scan]) || code[scan] == '_'))
                scan++;

            return scan;
        }

        private static int EndOfLine(string code, int index)
        {
            while (index < code.Length && code[index] != '\n')
                index++;

            return index;
        }

        private static int EndOfBlockComment(string code, int index)
        {
            int scan = index + 2;

            while (scan + 1 < code.Length && !(code[scan] == '*' && code[scan + 1] == '/'))
                scan++;

            return Mathf.Min(code.Length, scan + 2);
        }

        private static bool IsIdentifierStart(char value) =>
            char.IsLetter(value) || value == '_' || value == '@';

        /// <summary>
        /// A run of the snippet in one colour, or in none: a null tag is a name the highlighter
        /// has nothing to say about, and it takes the code style's own colour.
        /// </summary>
        private static int Colour(StringBuilder builder, string code, int start, int end, string open)
        {
            if (open == null)
                return Plain(builder, code, start, end);

            builder.Append(open);
            AppendSpan(builder, code, start, end);
            builder.Append("</color>");

            return end;
        }

        private static int Plain(StringBuilder builder, string code, int start, int end)
        {
            AppendSpan(builder, code, start, end);

            return end;
        }

        /// <summary>
        /// A run of the snippet, as it was written. An unterminated run - a string the snippet cut
        /// off - can report an end past the text, so the span is clamped rather than trusted.
        /// </summary>
        private static void AppendSpan(StringBuilder builder, string code, int start, int end)
        {
            int last = Mathf.Min(end, code.Length);

            if (last > start)
                builder.Append(code, start, last - start);
        }

        private static char Next(string code, int index) =>
            index + 1 < code.Length ? code[index + 1] : '\0';

        private static string Open(Color colour) => "<color=#" + ColorUtility.ToHtmlStringRGB(colour) + ">";
    }
}

#endif
