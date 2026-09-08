#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace FlowIoC.Editor.CodeGenerator
{
    /// <summary>
    /// Which of the shipped arities a generated function is built on. A function never derives from
    /// FunctionBody itself - its constructor is internal - so this is the whole set there is to
    /// choose from, and the choice decides the base type, the Execute signature and the usings.
    ///
    /// Every value carries its number, because the window remembers the last pick between domain
    /// reloads and a value inserted in the middle would silently change what a reader had selected.
    /// </summary>
    internal enum FunctionKind
    {
        Void = 0,
        Return = 1,
        Async = 2
    }

    /// <summary>
    /// One parameter of a generated function's Execute: the type as it is written in the file, and
    /// the name it is given.
    /// </summary>
    internal class FunctionParameter
    {
        public string Type = string.Empty;
        public string Name = string.Empty;
    }

    /// <summary>
    /// One injected member of a generated function. The namespace is resolved by whoever fills this
    /// in - the writer does no type lookup of its own, which is what keeps it testable without an
    /// AssetDatabase.
    /// </summary>
    internal class FunctionInjectable
    {
        public string Type = string.Empty;
        public string Namespace = string.Empty;
    }

    /// <summary>
    /// Everything Create Function asks for, in the shape the writer reads it.
    /// </summary>
    internal class FunctionScriptRequest
    {
        public string ClassName = string.Empty;
        public string Namespace = string.Empty;
        public FunctionKind Kind = FunctionKind.Void;

        /// <summary>
        /// What a Return function hands back, and what an Async function carries into its callback.
        /// A Void function has none, and an Async function that answers nothing leaves it empty.
        /// </summary>
        public string ReturnType = string.Empty;

        public List<FunctionParameter> Parameters = new List<FunctionParameter>();
        public List<FunctionInjectable> Injectables = new List<FunctionInjectable>();
    }

    /// <summary>
    /// Writes a function's file as text. There is no template file behind it, the way there is for
    /// a Command: a function's base type and its Execute signature both change with the arity, so
    /// eighteen templates would say what one builder says once.
    /// </summary>
    internal class FunctionScriptWriter
    {
        private const string INJECT_ATTRIBUTE_NAMESPACE = "FlowIoC.BaseModule.Injectable.Attributes";
        private const string VOID_FUNCTIONS_NAMESPACE = "FlowIoC.BaseModule.Function.VoidFunctions";
        private const string RETURN_FUNCTIONS_NAMESPACE = "FlowIoC.BaseModule.Function.ReturnableFunctions";
        private const string ASYNC_FUNCTIONS_NAMESPACE = "FlowIoC.BaseModule.Function.AsyncFunctions";

        /// <summary>
        /// The base type the function derives from, generic arguments and all. An Async function's
        /// type argument is the value its callback carries rather than a parameter of Execute,
        /// which is why the parameters are read for the other two kinds only.
        /// </summary>
        internal string BaseTypeFor(FunctionScriptRequest request)
        {
            switch (request.Kind)
            {
                case FunctionKind.Async:
                    return string.IsNullOrWhiteSpace(request.ReturnType)
                        ? "AsyncFunction"
                        : $"AsyncFunction<{request.ReturnType.Trim()}>";

                case FunctionKind.Return:
                    string returnType = string.IsNullOrWhiteSpace(request.ReturnType) ? "object" : request.ReturnType.Trim();
                    string returnArguments = JoinTypes(request, returnType);
                    return $"FunctionReturn<{returnArguments}>";

                default:
                    string voidArguments = JoinTypes(request, null);
                    return string.IsNullOrEmpty(voidArguments) ? "FunctionVoid" : $"FunctionVoid<{voidArguments}>";
            }
        }

        internal string Write(FunctionScriptRequest request)
        {
            var file = new StringBuilder();

            foreach (string usingLine in UsingsFor(request))
                file.AppendLine("using " + usingLine + ";");

            file.AppendLine();
            file.AppendLine("namespace " + request.Namespace);
            file.AppendLine("{");
            file.AppendLine($"    public class {request.ClassName} : {BaseTypeFor(request)}");
            file.AppendLine("    {");

            foreach (FunctionInjectable injectable in ValidInjectables(request))
                file.AppendLine($"        [Inject] private {injectable.Type} _{MemberNameFor(injectable.Type)} {{ get; set; }}");

            if (ValidInjectables(request).Count > 0)
                file.AppendLine();

            AppendExecute(file, request);

            file.AppendLine("    }");
            file.AppendLine("}");

            return file.ToString();
        }

        private void AppendExecute(StringBuilder file, FunctionScriptRequest request)
        {
            if (request.Kind == FunctionKind.Async)
            {
                file.AppendLine("        /// <summary>");
                file.AppendLine("        /// An async function answers through FunctionCompletedCallback rather than by returning,");
                file.AppendLine("        /// so a caller waiting for a value gets it there or not at all.");
                file.AppendLine("        /// </summary>");
                file.AppendLine("        public override IEnumerator Execute()");
                file.AppendLine("        {");
                file.AppendLine("            yield break;");
                file.AppendLine("        }");
                return;
            }

            string signature = request.Kind == FunctionKind.Return
                ? $"public override {ReturnTypeOf(request)} Execute({ParameterList(request)})"
                : $"public override void Execute({ParameterList(request)})";

            file.AppendLine("        " + signature);
            file.AppendLine("        {");

            if (request.Kind == FunctionKind.Return)
                file.AppendLine("            return default;");

            file.AppendLine("        }");
        }

        /// <summary>
        /// The usings the file needs, ordered the way the rest of the generated code orders them:
        /// System first, then FlowIoC, then whatever the injected types came from, each once.
        /// </summary>
        private List<string> UsingsFor(FunctionScriptRequest request)
        {
            var usings = new List<string>();

            if (request.Kind == FunctionKind.Async)
                usings.Add("System.Collections");

            usings.Add(request.Kind switch
            {
                FunctionKind.Async => ASYNC_FUNCTIONS_NAMESPACE,
                FunctionKind.Return => RETURN_FUNCTIONS_NAMESPACE,
                _ => VOID_FUNCTIONS_NAMESPACE
            });

            List<FunctionInjectable> injectables = ValidInjectables(request);
            if (injectables.Count > 0)
                usings.Add(INJECT_ATTRIBUTE_NAMESPACE);

            foreach (FunctionInjectable injectable in injectables)
            {
                if (usings.Contains(injectable.Namespace) || injectable.Namespace == request.Namespace) continue;

                usings.Add(injectable.Namespace);
            }

            return usings;
        }

        private List<FunctionInjectable> ValidInjectables(FunctionScriptRequest request)
        {
            var injectables = new List<FunctionInjectable>();

            foreach (FunctionInjectable injectable in request.Injectables)
            {
                if (string.IsNullOrWhiteSpace(injectable.Type) || string.IsNullOrWhiteSpace(injectable.Namespace)) continue;

                injectables.Add(injectable);
            }

            return injectables;
        }

        /// <summary>
        /// The member name for an injected type, the way the shipped modules write one: the
        /// interface's leading I dropped and the rest lower-cased, so IPlayerModel becomes
        /// _playerModel rather than the _iplayermodel a plain ToLower would give.
        /// </summary>
        private string MemberNameFor(string typeName)
        {
            string name = typeName.Trim();

            if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
                name = name.Substring(1);

            return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private string ReturnTypeOf(FunctionScriptRequest request) =>
            string.IsNullOrWhiteSpace(request.ReturnType) ? "object" : request.ReturnType.Trim();

        private string ParameterList(FunctionScriptRequest request)
        {
            var written = new List<string>();

            foreach (FunctionParameter parameter in ValidParameters(request))
                written.Add($"{parameter.Type.Trim()} {parameter.Name.Trim()}");

            return string.Join(", ", written);
        }

        /// <summary>
        /// The generic argument list of the base type: the return type first where there is one,
        /// then the parameter types in the order they were given.
        /// </summary>
        private string JoinTypes(FunctionScriptRequest request, string leadingType)
        {
            var types = new List<string>();

            if (!string.IsNullOrEmpty(leadingType))
                types.Add(leadingType);

            foreach (FunctionParameter parameter in ValidParameters(request))
                types.Add(parameter.Type.Trim());

            return string.Join(", ", types);
        }

        /// <summary>
        /// The parameters that can actually be written. An Async function's Execute takes none at
        /// all, and a row left half-filled in the window is not one.
        /// </summary>
        internal List<FunctionParameter> ValidParameters(FunctionScriptRequest request)
        {
            var parameters = new List<FunctionParameter>();

            if (request.Kind == FunctionKind.Async) return parameters;

            foreach (FunctionParameter parameter in request.Parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Type) || string.IsNullOrWhiteSpace(parameter.Name)) continue;

                parameters.Add(parameter);

                if (parameters.Count == MAX_PARAMETERS) break;
            }

            return parameters;
        }

        /// <summary>
        /// What the shipped arities go up to. A function that wants more takes a value object
        /// instead, which is what the fifth parameter was going to be anyway.
        /// </summary>
        internal const int MAX_PARAMETERS = 4;

        /// <summary>
        /// How a Command calls the function being written, in the shape the Help window's own
        /// example uses. This is the half of a function a generator cannot write for you - the
        /// file it produces says nothing about where it is called from, which is the whole
        /// difference between a Function and a Command - so the window shows it instead, ready to
        /// be copied into the Command that needs it.
        ///
        /// The parameter names are the ones typed into the window rather than invented, so the
        /// snippet reads as the call the author has in mind.
        /// </summary>
        internal string UsageFor(FunctionScriptRequest request)
        {
            var usage = new StringBuilder();
            usage.AppendLine("[Inject] private IFunctionProvider _functionProvider { get; set; }");
            usage.AppendLine();

            switch (request.Kind)
            {
                case FunctionKind.Async:
                    AppendAsyncUsage(usage, request);
                    break;

                case FunctionKind.Return:
                    AppendReturnUsage(usage, request);
                    break;

                default:
                    AppendVoidUsage(usage, request);
                    break;
            }

            return usage.ToString().TrimEnd();
        }

        private void AppendReturnUsage(StringBuilder usage, FunctionScriptRequest request)
        {
            usage.AppendLine($"var {ResultNameFor(request)} = _functionProvider");
            usage.AppendLine($"    .Call<{request.ClassName}>()");
            AppendParams(usage, request);
            usage.AppendLine($"    .ExecuteAndGetResult<{ReturnTypeOf(request)}>();");
        }

        /// <summary>
        /// A call with nothing to pass is one line, the way the Help window writes it; one that
        /// passes parameters breaks into the chain, because the arguments are what the reader is
        /// there to see.
        /// </summary>
        private void AppendVoidUsage(StringBuilder usage, FunctionScriptRequest request)
        {
            if (ValidParameters(request).Count == 0)
            {
                usage.AppendLine($"_functionProvider.Call<{request.ClassName}>().Execute();");
                return;
            }

            usage.AppendLine("_functionProvider");
            usage.AppendLine($"    .Call<{request.ClassName}>()");
            AppendParams(usage, request);
            usage.AppendLine("    .Execute();");
        }

        private void AppendAsyncUsage(StringBuilder usage, FunctionScriptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReturnType))
            {
                usage.AppendLine($"_functionProvider.CallAsync<{request.ClassName}>().ExecuteAsync();");
                return;
            }

            usage.AppendLine("_functionProvider");
            usage.AppendLine($"    .CallAsync<{request.ClassName}, {request.ReturnType.Trim()}>()");
            usage.AppendLine($"    .AddFunctionCompletedCallback({CallbackNameFor(request)})");
            usage.AppendLine("    .ExecuteAsync();");
        }

        private void AppendParams(StringBuilder usage, FunctionScriptRequest request)
        {
            List<FunctionParameter> parameters = ValidParameters(request);
            if (parameters.Count == 0) return;

            var names = new List<string>();

            foreach (FunctionParameter parameter in parameters)
                names.Add(parameter.Name.Trim());

            usage.AppendLine($"    .AddParams({string.Join(", ", names)})");
        }

        /// <summary>
        /// What the answer is called at the call site, and what the callback that receives it is
        /// called: both are the function's own name with the suffix off - CalculateDamageFunction
        /// answers into calculateDamage and reports to OnCalculateDamageCompleted.
        /// </summary>
        private string ResultNameFor(FunctionScriptRequest request)
        {
            string name = BareNameOf(request);

            return name.Length == 0 ? "result" : char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private string CallbackNameFor(FunctionScriptRequest request)
        {
            string name = BareNameOf(request);

            return name.Length == 0 ? "OnCompleted" : "On" + name + "Completed";
        }

        private string BareNameOf(FunctionScriptRequest request)
        {
            string name = request.ClassName ?? string.Empty;

            return name.EndsWith(FUNCTION_SUFFIX) ? name.Substring(0, name.Length - FUNCTION_SUFFIX.Length) : name;
        }

        private const string FUNCTION_SUFFIX = "Function";
    }
}
#endif