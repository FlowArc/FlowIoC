#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorUtils
    {
        private const string ATTRIBUTES_USING = "using FlowIoC.BaseModule.Attributes;";

        /// <summary>The indentation a line already carries, so a line written above it lines up.</summary>
        private static string LeadingWhitespace(string line) =>
            line.Substring(0, line.Length - line.TrimStart().Length);

        public static void CreateView(string viewName, string tempClassName, string viewPath, string tempClassPath,
            string namespaceName, List<string> actionsList, bool isTest)
        {
            var newViewPath = viewPath + "/" + viewName + ".cs";

            var tempViewContent = File.ReadAllLines(tempClassPath);
            var newViewContent = new List<string>();

            if (isTest)
                newViewContent.Add("#if UNITY_EDITOR");

            for (var ii = 0; ii < tempViewContent.Length; ii++)
            {
                var content = tempViewContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempClassName, viewName);
                }
                else if (content.Contains("//@Actions"))
                {
                    foreach (var actionName in actionsList)
                    {
                        newViewContent.Add("\t\tpublic Action " + actionName + ";");
                    }

                    continue;
                }

                newViewContent.Add(content);
            }

            if (isTest)
                newViewContent.Add("#endif");

            if (!Directory.Exists(viewPath)) Directory.CreateDirectory(viewPath);

            File.WriteAllLines(newViewPath, newViewContent.ToArray());
            AssetDatabase.Refresh();
            Highlight(newViewPath);
        }

        public static void CreateMediator(string mediatorName, string viewName, string tempClassName,
            string mediatorPath, string tempClassPath, string namespaceName, List<string> actionsList, bool isTest,
            string signalsClassName = null, string signalsNamespace = null)
        {
            var newMediatorPath = mediatorPath + "/" + mediatorName + ".cs";

            var tempMediatorContent = File.ReadAllLines(tempClassPath);
            var newMediatorContent = new List<string>();

            if (isTest)
                newMediatorContent.Add("#if UNITY_EDITOR");

            for (var ii = 0; ii < tempMediatorContent.Length; ii++)
            {
                var content = tempMediatorContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempClassName, mediatorName);
                }
                else if (content.Contains("[Inject]"))
                {
                    content = "\t\t[Inject] private " + viewName + " _view { get; set; }";
                }
                else if (content.Contains("//@Signals"))
                {
                    if (!string.IsNullOrEmpty(signalsClassName))
                    {
                        newMediatorContent.Add("\t\t[InjectSignal] private " + signalsClassName + " _signals { get; set; }");
                    }

                    continue;
                }
                else if (content.Contains("//@Register"))
                {
                    foreach (var actionName in actionsList)
                    {
                        var line = "\t\t\t_view." + actionName + " += On" + actionName + ";";
                        newMediatorContent.Add(line);
                    }

                    continue;
                }
                else if (content.Contains("//@Remove"))
                {
                    foreach (var actionName in actionsList)
                    {
                        var line = "\t\t\t_view." + actionName + " -= On" + actionName + ";";
                        newMediatorContent.Add(line);
                    }

                    continue;
                }
                else if (content.Contains("//@Methods"))
                {
                    foreach (var actionName in actionsList)
                    {
                        var line = "\t\tprivate void On" + actionName + "()";
                        newMediatorContent.Add(line);
                        newMediatorContent.Add("\t\t{");
                        newMediatorContent.Add("\t\t}");
                        newMediatorContent.Add("");
                    }

                    newMediatorContent.RemoveAt(newMediatorContent.Count - 1);
                    continue;
                }

                newMediatorContent.Add(content);
            }

            if (isTest)
                newMediatorContent.Add("#endif");

            if (!string.IsNullOrEmpty(signalsClassName))
                InsertUsing(newMediatorContent, signalsNamespace);

            if (!Directory.Exists(mediatorPath)) Directory.CreateDirectory(mediatorPath);

            File.WriteAllLines(newMediatorPath, newMediatorContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Writes a module's context from one of the templates. A context that will have a Root of
        /// its own is kept out of the Root inspector's Add Sub Context list, so a module meant to
        /// be hosted on another module's Root asks for <c>allowAsSubContext</c> and gets the
        /// attribute that puts it back.
        /// </summary>
        public static void CreateContext(string contextName, string tempClassName, string contextPath,
            string tempClassPath, string namespaceName, bool isScreen, bool isTest,
            bool allowAsSubContext = false)
        {
            var directoryPath = contextPath;
            var path = directoryPath + "/" + contextName + ".cs";

            var tempContextContent = File.ReadAllLines(tempClassPath);
            var newContextContent = new List<string>();

            if (isTest)
                newContextContent.Add("#if UNITY_EDITOR");

            for (var ii = 0; ii < tempContextContent.Length; ii++)
            {
                var content = tempContextContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    if (allowAsSubContext)
                        newContextContent.Add(IndentationOf(content) + "[AllowAsSubContext]");

                    content = content.Replace("internal class", "public class");
                }

                content = content.Replace(tempClassName, contextName);
                newContextContent.Add(content);
            }

            if (isTest)
                newContextContent.Add("#endif");

            if (allowAsSubContext)
                InsertUsing(newContextContent, "FlowIoC.BaseModule.Attributes");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllLines(path, newContextContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <param name="attribute">
        /// An attribute to write above the class, such as <c>[FlowHeader(FlowRole.Core)]</c>, or
        /// null for none. A Core module is the case that needs it: it carries no role suffix, so
        /// the attribute is the only thing that can tell the inspector what the Root roots.
        /// </param>
        public static void CreateRoot(string rootName, string contextName, string tempContextName, string tempRootName,
            string rootPath, string tempClassPath, string namespaceName, bool isTest, string attribute = null)
        {
            var directoryPath = rootPath;
            var path = directoryPath + "/" + rootName + ".cs";

            var tempRootContent = File.ReadAllLines(tempClassPath);
            var newRootContent = new List<string>();

            if (isTest)
                newRootContent.Add("#if UNITY_EDITOR");

            for (var ii = 0; ii < tempRootContent.Length; ii++)
            {
                var content = tempRootContent[ii];

                // The template carries the attributes using so a Core Root can be written without
                // touching it here. Every other Root drops the line rather than being generated
                // with a using nothing in the file needs.
                if (attribute == null && content.Contains(ATTRIBUTES_USING))
                    continue;

                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempRootName, rootName);
                    content = content.Replace(tempContextName, contextName);

                    if (attribute != null)
                        newRootContent.Add(LeadingWhitespace(content) + attribute);
                }

                newRootContent.Add(content);
            }

            if (isTest)
                newRootContent.Add("#endif");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllLines(path, newRootContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// <paramref name="isDummy"/> and <paramref name="isTest"/> each wrap the file in UNITY_EDITOR:
        /// a dummy model exists for the Editor and never ships, and every script in a test module
        /// is wrapped, or one unwrapped file would carry the whole module into a player build.
        /// </summary>
        public static void CreateModel(string modelName, string tempClassName, string modelPath,
            string tempClassPath, string namespaceName, List<string> injectables, bool isDummy, bool isTest = false)
        {
            var newViewPath = modelPath + "/" + modelName + ".cs";

            var tempModelContent = File.ReadAllLines(tempClassPath);
            var newModelContent = new List<string>();
            var usingsToAdd = new HashSet<string>();

            bool wrap = isDummy || isTest;

            for (var ii = 0; ii < tempModelContent.Length; ii++)
            {
                var content = tempModelContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempClassName, modelName);

                    if (isDummy)
                    {
                        string baseModelName = modelName.Replace("Dummy", "");
                        content = content.Replace($"I{modelName}", $"I{baseModelName}");
                    }
                    else
                    {
                        content = content.Replace($"I{tempClassName}", $"I{modelName}");
                    }
                }
                else if (content.Contains("//@Injectables"))
                {
                    WriteInjectables(injectables, newModelContent, usingsToAdd);

                    continue;
                }

                newModelContent.Add(content);
            }

            if (!Directory.Exists(modelPath)) Directory.CreateDirectory(modelPath);

            List<string> finalContent = Wrapped(usingsToAdd, newModelContent, wrap);

            File.WriteAllLines(newViewPath, finalContent.ToArray());
            AssetDatabase.Refresh();
            Highlight(newViewPath);
        }

        public static void CreateModelInterface(string modelName, string tempClassName, string modelPath,
            string tempClassPath, string namespaceName, bool isTest = false)
        {
            var newViewPath = modelPath + "/" + modelName + ".cs";

            var tempViewContent = File.ReadAllLines(tempClassPath);
            var newViewContent = new List<string>();

            for (var ii = 0; ii < tempViewContent.Length; ii++)
            {
                var content = tempViewContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal interface "))
                {
                    content = content.Replace("internal interface", "public interface");
                    content = content.Replace(tempClassName, modelName);
                }

                newViewContent.Add(content);
            }

            if (!Directory.Exists(modelPath)) Directory.CreateDirectory(modelPath);

            File.WriteAllLines(newViewPath, Wrapped(new List<string>(), newViewContent, isTest).ToArray());
            AssetDatabase.Refresh();
        }

        public static void CreateCommand(string commandName, string tempClassName, string commandPath,
            string tempClassPath, string namespaceName, List<string> injectables, bool isTest = false)
        {
            var newViewPath = commandPath + "/" + commandName + ".cs";

            var tempViewContent = File.ReadAllLines(tempClassPath);
            var newViewContent = new List<string>();
            var usingsToAdd = new HashSet<string>();

            for (var ii = 0; ii < tempViewContent.Length; ii++)
            {
                var content = tempViewContent[ii];
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempClassName, commandName);
                }
                else if (content.Contains("//@Injectables"))
                {
                    WriteInjectables(injectables, newViewContent, usingsToAdd);

                    continue;
                }

                newViewContent.Add(content);
            }

            if (!Directory.Exists(commandPath)) Directory.CreateDirectory(commandPath);

            List<string> finalContent = Wrapped(usingsToAdd, newViewContent, isTest);

            File.WriteAllLines(newViewPath, finalContent.ToArray());
            AssetDatabase.Refresh();
            Highlight(newViewPath);
        }

        /// <summary>
        /// Writes a module's signal holder from the TempSignals template. Unlike the other
        /// generators the template name is replaced on every line rather than only on the class
        /// declaration, because the holder names its own Incoming and Outgoing classes in field
        /// declarations too - and those lines carry no marker to key off.
        ///
        /// A test module compiles only in the Editor, so its holder is wrapped the way its Root and
        /// Context already are. One unwrapped file in zTestModules would carry the whole test module
        /// into a player build.
        /// </summary>
        /// <summary>
        /// Writes a signal holder from a template. <paramref name="makePublic"/> is what separates
        /// the two a module gets: the public holder in Shared is what every other module talks to
        /// it through, while the internal one stays internal because nothing outside the module's
        /// own assembly has any business dispatching it.
        /// </summary>
        public static void CreateSignals(string signalsName, string tempClassName, string signalsPath,
            string tempClassPath, string namespaceName, bool isTest, bool makePublic = true)
        {
            string newSignalsPath = signalsPath + "/" + signalsName + ".cs";

            string[] tempSignalsContent = File.ReadAllLines(tempClassPath);
            List<string> newSignalsContent = new List<string>();

            if (isTest)
                newSignalsContent.Add("#if UNITY_EDITOR");

            foreach (string line in tempSignalsContent)
            {
                string content = line;
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else
                {
                    if (makePublic)
                        content = content.Replace("internal class", "public class");

                    content = content.Replace(tempClassName, signalsName);
                }

                newSignalsContent.Add(content);
            }

            if (isTest)
                newSignalsContent.Add("#endif");

            if (!Directory.Exists(signalsPath)) Directory.CreateDirectory(signalsPath);

            File.WriteAllLines(newSignalsPath, newSignalsContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Declares the module's own signal holder on a Context and binds it in SignalBindings.
        /// A Context that owns its signals holds it as a plain field rather than an injected one -
        /// it is the thing doing the binding, so there is nothing to inject it from.
        /// </summary>
        public static void BindSignalsInContext(string contextPath, string signalsClassName, string signalsNamespace,
            string defaultFieldName = "_signals")
        {
            if (!File.Exists(contextPath)) return;

            List<string> contextLines = File.ReadAllLines(contextPath).ToList();

            if (contextLines.Any(line => line.Contains($"InjectionBinderCrossContext.Bind<{signalsClassName}>()"))) return;

            string fieldName = ResolveSignalFieldName(contextLines, signalsClassName, defaultFieldName);
            bool fieldDeclared = contextLines.Any(line => line.Contains($"{signalsClassName} {fieldName}"));

            List<string> newContextContent = new List<string>();

            foreach (string line in contextLines)
            {
                if (!fieldDeclared && line.Contains("public override void SignalBindings()"))
                {
                    newContextContent.Add($"\t\tprivate {signalsClassName} {fieldName};");
                    newContextContent.Add("");
                    fieldDeclared = true;
                }

                newContextContent.Add(line);

                if (line.Contains("base.SignalBindings();"))
                {
                    newContextContent.Add($"\t\t\t{fieldName} = InjectionBinderCrossContext.Bind<{signalsClassName}>();");
                }
            }

            InsertUsing(newContextContent, signalsNamespace);

            File.WriteAllLines(contextPath, newContextContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// The name the context already knows a signal holder by, or <paramref name="defaultFieldName"/>
        /// when it does not know one yet. A context that owns its holder declares it as
        /// <c>_signals</c>, so the command generator has to write that name into the bindings it
        /// appends rather than the one it would have picked for itself.
        /// </summary>
        private static string ResolveSignalFieldName(IEnumerable<string> contextLines, string signalClassName,
            string defaultFieldName = null)
        {
            Match match = contextLines
                .Select(line => Regex.Match(line, $@"\b{Regex.Escape(signalClassName)}\s+(_\w+)"))
                .FirstOrDefault(candidate => candidate.Success);

            if (match != null) return match.Groups[1].Value;

            return string.IsNullOrEmpty(defaultFieldName) ? "_" + signalClassName.ToLower() : defaultFieldName;
        }

        /// <summary>
        /// Adds a using to generated content, after the leading <c>#if UNITY_EDITOR</c> when the
        /// file has one - a test-only using written above the guard would break player builds.
        /// </summary>
        /// <summary>The whitespace a line starts with, so a line written above it lines up.</summary>
        private static string IndentationOf(string line) =>
            line.Substring(0, line.Length - line.TrimStart().Length);

        private static void InsertUsing(List<string> content, string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName)) return;

            string usingLine = $"using {namespaceName};";
            if (content.Any(line => line.Trim() == usingLine)) return;

            int insertIndex = content.Count > 0 && content[0].TrimStart().StartsWith("#if") ? 1 : 0;
            content.Insert(insertIndex, usingLine);
        }

        public static void BindMediationInContext(string contextPath, string viewName, string mediationName, string viewNamespace)
        {
            var contextLines = File.ReadAllLines(contextPath);
            var newRootContent = new List<string>();

            if (newRootContent.Contains("#if UNITY_EDITOR"))
            {
                newRootContent.Add("#if UNITY_EDITOR");
                newRootContent.Add("using " + viewNamespace + ";");
            }
            else
            {
                newRootContent.Add("using " + viewNamespace + ";");
            }

            for (var ii = 0; ii < contextLines.Length; ii++)
            {
                var content = contextLines[ii];

                if (content.Contains("base.MediationBindings();"))
                {
                    newRootContent.Add(content);

                    newRootContent.Add("\t\t\t" + $"MediationBinder.Bind<{viewName}>().To<{mediationName}>();");
                    continue;
                }

                newRootContent.Add(content);
            }

            File.WriteAllLines(contextPath, newRootContent.ToArray());
            AssetDatabase.Refresh();
        }

        public static void BindModelInContext(string contextPath, string modelName, string iModelName, string dummyModelName, string modelNamespace,
            bool useDummyBinding = false)
        {
            var contextLines = File.ReadAllLines(contextPath);
            var newRootContent = new List<string>();

            if (newRootContent.Contains("#if UNITY_EDITOR"))
            {
                newRootContent.Add("#if UNITY_EDITOR");
                newRootContent.Add("using " + modelNamespace + ";");
            }
            else
            {
                newRootContent.Add("using " + modelNamespace + ";");
            }

            for (var ii = 0; ii < contextLines.Length; ii++)
            {
                var content = contextLines[ii];

                if (content.Contains("base.InjectionBindings();"))
                {
                    newRootContent.Add(content);

                    if (useDummyBinding)
                    {
                        newRootContent.Add("\t\t\t" + $"InjectionBinder.Bind<{iModelName}, {modelName},{dummyModelName} >();");
                    }
                    else
                    {
                        newRootContent.Add("\t\t\t" + $"InjectionBinder.Bind<{iModelName},{modelName}>();");
                    }

                    continue;
                }

                newRootContent.Add(content);
            }

            File.WriteAllLines(contextPath, newRootContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Writes the screen's first Open into a test context's Launch, and the using the screen
        /// view needs. The using is this method's business because nothing else in the test
        /// context names a type from the screen module.
        /// </summary>
        public static void ShowScreenInLaunch(string contextPath, string screenName, string viewNamespace)
        {
            if (!File.Exists(contextPath)) return;

            var contextLines = File.ReadAllLines(contextPath);
            var newRootContent = new List<string>();

            for (var ii = 0; ii < contextLines.Length; ii++)
            {
                var content = contextLines[ii];
                if (content.Contains("base.Launch();"))
                {
                    newRootContent.Add(content);
                    newRootContent.Add($"\t\t\t_screenService.Open<{screenName}>().Show();\n");
                    continue;
                }

                newRootContent.Add(content);
            }

            InsertUsing(newRootContent, viewNamespace);

            File.WriteAllLines(contextPath, newRootContent.ToArray());
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Writes a function's file into the module's Controllers folder, where the Commands are.
        /// Unlike the Command generator there is no template behind it and nothing to bind: a
        /// function is called from inside a Command rather than dispatched, so no Context is
        /// touched and the whole job is the one file.
        ///
        /// The injected types arrive as names and their namespaces are looked up here, so the
        /// writer itself stays a string builder that a test can drive without an AssetDatabase.
        /// </summary>
        internal static void CreateFunction(FunctionScriptRequest request, IEnumerable<string> injectableTypeNames, string functionPath)
        {
            var index = new InjectableTypeIndex();

            foreach (string typeName in injectableTypeNames)
            {
                if (string.IsNullOrWhiteSpace(typeName)) continue;

                string name = typeName.Trim();

                // Written whether or not the project has the type. Dropping it was the old
                // behaviour and it dropped it silently, so a name with a typo in it simply was not
                // in the file and nothing said why. Written, the compiler says why.
                request.Injectables.Add(new FunctionInjectable
                {
                    Type = name,
                    Namespace = index.NamespaceFor(name) ?? string.Empty
                });
            }

            if (!Directory.Exists(functionPath)) Directory.CreateDirectory(functionPath);

            string writtenPath = Path.Combine(functionPath, request.ClassName + ".cs");

            File.WriteAllText(writtenPath, new FunctionScriptWriter().Write(request));
            AssetDatabase.Refresh();

            Highlight(writtenPath);
        }

        private static string FindNamespaceForType(string typeName) => new InjectableTypeIndex().NamespaceFor(typeName);

        /// <summary>
        /// Writes the injected members a window asked for.
        ///
        /// Every name given is written. A name whose type the project has brings its using with it;
        /// one it does not have is written anyway, because the alternative is what this replaced -
        /// the member silently missing from the file, with the author left to work out that a typo
        /// three fields up was the reason. Nothing is logged about it either way: an unresolved
        /// type is a compiler error naming the file, the line and the column, and a warning
        /// alongside it would say the same thing less precisely.
        /// </summary>
        private static void WriteInjectables(IEnumerable<string> injectables, List<string> lines, HashSet<string> usingsToAdd)
        {
            var index = new InjectableTypeIndex();

            foreach (string injectableName in injectables)
            {
                if (string.IsNullOrWhiteSpace(injectableName)) continue;

                string typeName = injectableName.Trim();
                string injectableNamespace = index.NamespaceFor(typeName);

                if (!string.IsNullOrEmpty(injectableNamespace))
                    usingsToAdd.Add($"using {injectableNamespace};");

                lines.Add($"\t\t[Inject] private {typeName} _{MemberNameFor(typeName)} {{ get; set; }}");
            }
        }

        /// <summary>
        /// The member name for an injected type, the way the shipped modules write one: the
        /// interface's leading I dropped and the first letter lowered, so ICounterService becomes
        /// _counterService rather than the _icounterservice a plain ToLower gives.
        /// </summary>
        private static string MemberNameFor(string typeName)
        {
            string name = typeName;

            if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
                name = name.Substring(1);

            return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Selects the file that was just written and pings it in the Project window. A generator
        /// writes into a folder the author may not have open, and a file they cannot find reads as
        /// a generator that did nothing - which is the same complaint a silently dropped injectable
        /// makes. The path is turned into one relative to the project, because that is the only
        /// kind AssetDatabase loads.
        /// </summary>
        /// <summary>
        /// The file as it is written: the usings the injectables asked for, then the body, the
        /// whole of it inside UNITY_EDITOR when the module the file goes into compiles only there.
        /// The directive goes above the usings, not between them and the body - a using outside
        /// the directive is a reference a player build still has to resolve.
        /// </summary>
        private static List<string> Wrapped(IEnumerable<string> usings, List<string> body, bool wrap)
        {
            var file = new List<string>();

            if (wrap) file.Add("#if UNITY_EDITOR");

            file.AddRange(usings);
            file.AddRange(body);

            if (wrap) file.Add("#endif");

            return file;
        }

        internal static void Highlight(string writtenPath)
        {
            if (string.IsNullOrEmpty(writtenPath)) return;

            string full = Path.GetFullPath(writtenPath).Replace('\\', '/');
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');

            if (!full.StartsWith(project)) return;

            string relative = full.Substring(project.Length).TrimStart('/');
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relative);

            if (asset == null) return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif