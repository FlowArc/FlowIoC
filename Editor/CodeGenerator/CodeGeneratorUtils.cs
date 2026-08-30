#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace FlowIoC.Editor.CodeGenerator
{
    internal static class CodeGeneratorUtils
    {
        private enum CommandEndingType
        {
            None,
            InSequence,
            InParallel
        }

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

        public static void CreateContext(string contextName, string tempClassName, string contextPath,
            string tempClassPath, string namespaceName, bool isScreen, bool isTest)
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
                    content = content.Replace("internal class", "public class");
                }

                content = content.Replace(tempClassName, contextName);
                newContextContent.Add(content);
            }

            if (isTest)
                newContextContent.Add("#endif");

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllLines(path, newContextContent.ToArray());
            AssetDatabase.Refresh();
        }

        public static void CreateRoot(string rootName, string contextName, string tempContextName, string tempRootName,
            string rootPath, string tempClassPath, string namespaceName, bool isTest)
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
                if (content.Contains("namespace "))
                {
                    content = "namespace " + namespaceName;
                }
                else if (content.Contains("internal class "))
                {
                    content = content.Replace("internal class", "public class");
                    content = content.Replace(tempRootName, rootName);
                    content = content.Replace(tempContextName, contextName);
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

        public static void CreateModel(string modelName, string tempClassName, string modelPath,
            string tempClassPath, string namespaceName, List<string> injectables, bool isDummy)
        {
            var newViewPath = modelPath + "/" + modelName + ".cs";

            var tempModelContent = File.ReadAllLines(tempClassPath);
            var newModelContent = new List<string>();
            var usingsToAdd = new HashSet<string>();

            if (isDummy)
                newModelContent.Add("#if UNITY_EDITOR");

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
                    foreach (string injectableName in injectables)
                    {
                        string injectableNamespace = FindNamespaceForType(injectableName);
                        if (!string.IsNullOrEmpty(injectableNamespace))
                        {
                            string getset = "{ get; set; }";
                            usingsToAdd.Add($"using {injectableNamespace};");
                            newModelContent.Add($"\t\t[Inject] private {injectableName} _{injectableName.ToLower()} {getset}");
                        }
                    }

                    continue;
                }

                newModelContent.Add(content);
            }

            if (isDummy)
                newModelContent.Add("#endif");

            if (!Directory.Exists(modelPath)) Directory.CreateDirectory(modelPath);

            var finalContent = new List<string>(usingsToAdd);
            finalContent.AddRange(newModelContent);

            File.WriteAllLines(newViewPath, finalContent.ToArray());
            AssetDatabase.Refresh();
        }

        public static void CreateModelInterface(string modelName, string tempClassName, string modelPath,
            string tempClassPath, string namespaceName)
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

            File.WriteAllLines(newViewPath, newViewContent.ToArray());
            AssetDatabase.Refresh();
        }

        public static void CreateCommand(string commandName, string tempClassName, string commandPath,
            string tempClassPath, string namespaceName, List<string> injectables)
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
                    foreach (string injectableName in injectables)
                    {
                        string injectableNamespace = FindNamespaceForType(injectableName);
                        if (!string.IsNullOrEmpty(injectableNamespace))
                        {
                            string getset = "{ get; set; }";
                            usingsToAdd.Add($"using {injectableNamespace};");
                            newViewContent.Add($"\t\t[Inject] private {injectableName} _{injectableName.ToLower()} {getset}");
                        }
                    }

                    continue;
                }

                newViewContent.Add(content);
            }

            if (!Directory.Exists(commandPath)) Directory.CreateDirectory(commandPath);

            var finalContent = new List<string>(usingsToAdd);
            finalContent.AddRange(newViewContent);

            File.WriteAllLines(newViewPath, finalContent.ToArray());
            AssetDatabase.Refresh();
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

        public static void BindMediationInScreenContext(string contextPath, string viewNamespace)
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

                    //newRootContent.Add("\t\t\t" + $"MediationBinder.Bind<{viewName}>().To<{mediationName}>();");
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

        public static void InjectSignalInContext(string contextPath, string signalClassName)
        {
            string[] contextLines = File.ReadAllLines(contextPath);

            List<string> newRootContent = new List<string>();

            HashSet<string> usingsToAdd = new HashSet<string>();

            string injectableNamespace = FindNamespaceForType(signalClassName);
            if (!string.IsNullOrEmpty(injectableNamespace))
            {
                usingsToAdd.Add($"using {injectableNamespace};");
            }

            // A context that binds the holder itself already declares it as a plain field, so the
            // match is on the declaration rather than on the [Inject] that only one of the two
            // shapes carries - otherwise a second field of the same type lands beside it and the
            // bindings written below would name a field nothing ever assigns.
            bool alreadyInjected = contextLines.Any(line =>
                Regex.IsMatch(line, $@"\b{Regex.Escape(signalClassName)}\s+_\w+")
            );

            for (int ii = 0; ii < contextLines.Length; ii++)
            {
                string content = contextLines[ii];

                if (content.Contains("public override void SignalBindings()"))
                {
                    if (!alreadyInjected)
                    {
                        newRootContent.Add("\t\t[Inject] private " + signalClassName + " _" + signalClassName.ToLower() + ";");
                    }
                }

                newRootContent.Add(content);
            }

            List<string> finalContent = new List<string>(usingsToAdd);
            finalContent.AddRange(newRootContent);

            File.WriteAllLines(contextPath, finalContent.ToArray());
            AssetDatabase.Refresh();
        }


        public static void BindCommandInContext(
            string contextPath,
            string commandName,
            string signalClassName,
            string signalName,
            string commandNamespace,
            bool isSequence)
        {
            List<string> allLines = File.ReadAllLines(contextPath).ToList();
            List<string> newRootContent = new List<string>();

            bool hasUnityEditor = allLines.Any(line => line.Contains("#if UNITY_EDITOR"));

            if (hasUnityEditor)
            {
                newRootContent.Add("#if UNITY_EDITOR");
                newRootContent.Add($"using {commandNamespace};");
            }
            else
            {
                newRootContent.Add($"using {commandNamespace};");
            }

            bool inBlock = false;
            List<string> blockLines = new List<string>();
            bool foundAnyBlock = false;

            string fullSignal = $"{ResolveSignalFieldName(allLines, signalClassName)}.{signalName}";

            for (int i = 0; i < allLines.Count; i++)
            {
                string line = allLines[i];

                if (!inBlock)
                {
                    if (line.Contains("CommandBinder.Bind("))
                    {
                        inBlock = true;
                        blockLines.Clear();
                        blockLines.Add(line);
                        foundAnyBlock = true;
                    }
                    else
                    {
                        newRootContent.Add(line);
                    }
                }
                else
                {
                    blockLines.Add(line);

                    if (line.Contains(");"))
                    {
                        inBlock = false;

                        string updatedBlock = ProcessBlock(
                            blockLines,
                            fullSignal,
                            commandName,
                            isSequence
                        );

                        newRootContent.Add(updatedBlock);
                    }
                }
            }

            if (!foundAnyBlock)
            {
                int baseIndex = newRootContent.FindIndex(x => x.Contains("base.CommandBindings();"));
                if (baseIndex >= 0)
                {
                    newRootContent.Insert(baseIndex + 1, CreateNewBlock(fullSignal, commandName, isSequence));
                }
                else
                {
                    newRootContent.Add("");
                    newRootContent.Add("// (No CommandBinder found, adding a new block automatically)");
                    newRootContent.Add(CreateNewBlock(fullSignal, commandName, isSequence));
                }
            }

            File.WriteAllLines(contextPath, newRootContent);
            AssetDatabase.Refresh();
        }

        private static string ProcessBlock(
            List<string> blockLines,
            string targetSignal,
            string newCommand,
            bool isSequence)
        {
            string joinedBlock = string.Join("\n", blockLines);
            bool blockHasOurSignal = joinedBlock.Contains($"Bind({targetSignal})");

            Regex toRegex = new Regex(@"\.To<([^>]+)>\(\)", RegexOptions.Multiline);
            MatchCollection matches = toRegex.Matches(joinedBlock);
            List<string> commands = new List<string>();
            foreach (Match m in matches)
            {
                string cmd = m.Groups[1].Value.Trim();
                if (!commands.Contains(cmd))
                    commands.Add(cmd);
            }

            bool hadSequence = joinedBlock.Contains(".InSequence()");
            bool hadParallel = joinedBlock.Contains(".InParallel()");

            if (blockHasOurSignal && !string.IsNullOrEmpty(newCommand))
            {
                if (!commands.Contains(newCommand))
                {
                    commands.Add(newCommand);
                }
            }

            int cmdCount = commands.Count;
            CommandEndingType finalCommandEnding = CommandEndingType.None;

            if (hadSequence) finalCommandEnding = CommandEndingType.InSequence;
            else if (hadParallel) finalCommandEnding = CommandEndingType.InParallel;

            if (finalCommandEnding == CommandEndingType.None && cmdCount >= 2)
            {
                finalCommandEnding = isSequence ? CommandEndingType.InSequence : CommandEndingType.InParallel;
            }

            if (cmdCount <= 1)
            {
                finalCommandEnding = CommandEndingType.None;
            }

            return BuildBlock(targetSignal, blockHasOurSignal, commands, finalCommandEnding, joinedBlock);
        }

        private static string BuildBlock(
            string targetSignal,
            bool blockHasOurSignal,
            List<string> commands,
            CommandEndingType commandEnding,
            string oldBlock)
        {
            if (!blockHasOurSignal)
            {
                return oldBlock;
            }

            if (commands.Count == 0)
            {
                return oldBlock;
            }

            List<string> sb = new List<string> {$"CommandBinder.Bind({targetSignal})"};

            foreach (string cmd in commands)
            {
                sb.Add($"    .To<{cmd}>()");
            }

            if (commandEnding == CommandEndingType.InSequence)
            {
                sb.Add("    .InSequence()");
            }
            else if (commandEnding == CommandEndingType.InParallel)
            {
                sb.Add("    .InParallel()");
            }

            int lastIndex = sb.Count - 1;
            sb[lastIndex] += ";";

            return string.Join("\r\n", sb);
        }

        private static string CreateNewBlock(string fullSignal, string commandName, bool isSequence)
        {
            List<string> lines = new List<string> {$"CommandBinder.Bind({fullSignal})"};

            if (!string.IsNullOrEmpty(commandName))
            {
                lines.Add($"    .To<{commandName}>()");
            }

            lines.Add(isSequence ? "    .InSequence();" : "    .InParallel();");

            return string.Join("\r\n", lines);
        }

        public static void ShowScreenInLaunch(string contextPath, string screenName, string screenType, string parentFolderName)
        {
            var contextLines = File.ReadAllLines(contextPath);
            var newRootContent = new List<string>();

            //newRootContent.Add($"using {parentFolderName}.Contexts.Screen.Enums;");

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

            File.WriteAllLines(contextPath, newRootContent.ToArray());
            AssetDatabase.Refresh();
        }

        private static string FindNamespaceForType(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null)
                {
                    return type.Namespace;
                }
            }

            return null;
        }
    }
}
#endif