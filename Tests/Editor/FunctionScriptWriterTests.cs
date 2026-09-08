using System.Collections.Generic;
using FlowIoC.Editor.CodeGenerator;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What Create Function writes. The writer is the half of the generator that decides the base
    /// type, the Execute signature and the usings, and those three have to agree with one another -
    /// which is the mistake a hand-written function makes and the reason the generator exists.
    /// </summary>
    public class FunctionScriptWriterTests
    {
        private FunctionScriptWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _writer = new FunctionScriptWriter();
        }

        private static FunctionScriptRequest Request(FunctionKind kind, string returnType = "", params string[] parameterTypes)
        {
            var request = new FunctionScriptRequest
            {
                ClassName = "CalculateDamageFunction",
                Namespace = "Modules.Player.Controllers",
                Kind = kind,
                ReturnType = returnType
            };

            for (int i = 0; i < parameterTypes.Length; i++)
                request.Parameters.Add(new FunctionParameter {Type = parameterTypes[i], Name = "param" + i});

            return request;
        }

        [Test]
        public void A_void_function_with_no_parameters_derives_from_the_bare_arity()
        {
            Assert.That(_writer.BaseTypeFor(Request(FunctionKind.Void)), Is.EqualTo("FunctionVoid"));
        }

        [Test]
        public void A_void_function_carries_its_parameter_types_into_the_base()
        {
            string baseType = _writer.BaseTypeFor(Request(FunctionKind.Void, string.Empty, "string", "int"));

            Assert.That(baseType, Is.EqualTo("FunctionVoid<string, int>"));
        }

        /// <summary>
        /// The return type leads the base's generic arguments and the parameters follow, which is
        /// the order FunctionReturn declares them in.
        /// </summary>
        [Test]
        public void A_returning_function_puts_its_return_type_first()
        {
            string baseType = _writer.BaseTypeFor(Request(FunctionKind.Return, "double", "string"));

            Assert.That(baseType, Is.EqualTo("FunctionReturn<double, string>"));
        }

        /// <summary>
        /// An async function's type argument is the value its callback carries, not a parameter of
        /// Execute - so parameters given to one are dropped rather than written into a signature
        /// the base does not have.
        /// </summary>
        [Test]
        public void An_async_function_takes_no_execute_parameters()
        {
            FunctionScriptRequest request = Request(FunctionKind.Async, "Profile", "string");

            Assert.That(_writer.BaseTypeFor(request), Is.EqualTo("AsyncFunction<Profile>"));
            Assert.That(_writer.ValidParameters(request), Is.Empty);
            Assert.That(_writer.Write(request), Does.Contain("public override IEnumerator Execute()"));
        }

        [Test]
        public void An_async_function_that_answers_nothing_takes_the_arity_with_no_value()
        {
            Assert.That(_writer.BaseTypeFor(Request(FunctionKind.Async)), Is.EqualTo("AsyncFunction"));
        }

        [Test]
        public void A_returning_function_writes_a_signature_that_matches_its_base()
        {
            string file = _writer.Write(Request(FunctionKind.Return, "double", "string"));

            Assert.That(file, Does.Contain("public class CalculateDamageFunction : FunctionReturn<double, string>"));
            Assert.That(file, Does.Contain("public override double Execute(string param0)"));
            Assert.That(file, Does.Contain("return default;"));
        }

        [Test]
        public void The_file_imports_the_namespace_the_base_type_lives_in()
        {
            Assert.That(_writer.Write(Request(FunctionKind.Void)),
                Does.Contain("using FlowIoC.BaseModule.Function.VoidFunctions;"));

            Assert.That(_writer.Write(Request(FunctionKind.Return, "int")),
                Does.Contain("using FlowIoC.BaseModule.Function.ReturnableFunctions;"));

            string asyncFile = _writer.Write(Request(FunctionKind.Async));
            Assert.That(asyncFile, Does.Contain("using System.Collections;"));
            Assert.That(asyncFile, Does.Contain("using FlowIoC.BaseModule.Function.AsyncFunctions;"));
        }

        /// <summary>
        /// An injected member is named the way the shipped modules name one: the interface's
        /// leading I dropped, so IPlayerModel becomes _playerModel rather than _iplayermodel.
        /// </summary>
        [Test]
        public void An_injectable_is_written_as_a_property_named_off_its_interface()
        {
            FunctionScriptRequest request = Request(FunctionKind.Void);
            request.Injectables.Add(new FunctionInjectable {Type = "IPlayerModel", Namespace = "Modules.Player.Models"});

            string file = _writer.Write(request);

            Assert.That(file, Does.Contain("[Inject] private IPlayerModel _playerModel { get; set; }"));
            Assert.That(file, Does.Contain("using FlowIoC.BaseModule.Injectable.Attributes;"));
            Assert.That(file, Does.Contain("using Modules.Player.Models;"));
        }

        /// <summary>
        /// An injectable whose type the project does not have is written all the same, without a
        /// using. Dropping it was the old behaviour and it dropped it silently: the member the
        /// author had just asked for was simply not in the file, and nothing said why. A member
        /// that does not compile is a report naming the line; nothing at all is not.
        /// </summary>
        [Test]
        public void An_injectable_with_no_namespace_is_still_written()
        {
            FunctionScriptRequest request = Request(FunctionKind.Void);
            request.Injectables.Add(new FunctionInjectable {Type = "ITypoModel", Namespace = string.Empty});

            string file = _writer.Write(request);

            Assert.That(file, Does.Contain("[Inject] private ITypoModel _typoModel { get; set; }"));
            Assert.That(file, Does.Contain("using FlowIoC.BaseModule.Injectable.Attributes;"));
        }

        [Test]
        public void A_file_with_no_injectables_does_not_import_the_inject_attribute()
        {
            Assert.That(_writer.Write(Request(FunctionKind.Void)),
                Does.Not.Contain("using FlowIoC.BaseModule.Injectable.Attributes;"));
        }

        /// <summary>
        /// The shipped arities go up to four parameters, so a fifth is dropped rather than written
        /// into a base type that does not exist.
        /// </summary>
        [Test]
        public void A_fifth_parameter_is_dropped()
        {
            FunctionScriptRequest request = Request(FunctionKind.Void, string.Empty, "int", "int", "int", "int", "int");

            Assert.That(_writer.ValidParameters(request), Has.Count.EqualTo(4));
            Assert.That(_writer.BaseTypeFor(request), Is.EqualTo("FunctionVoid<int, int, int, int>"));
        }

        [Test]
        public void A_half_filled_parameter_row_is_not_written()
        {
            FunctionScriptRequest request = Request(FunctionKind.Void);
            request.Parameters.Add(new FunctionParameter {Type = "int", Name = string.Empty});
            request.Parameters.Add(new FunctionParameter {Type = string.Empty, Name = "count"});

            Assert.That(_writer.ValidParameters(request), Is.Empty);
            Assert.That(_writer.BaseTypeFor(request), Is.EqualTo("FunctionVoid"));
        }

        /// <summary>
        /// The snippet the window shows is the half a generator cannot write: where the function is
        /// called from. It has to agree with the class in the same window, so the terminator, the
        /// arguments and the type parameter are all read off the same request.
        /// </summary>
        [Test]
        public void A_returning_function_is_called_with_its_arguments_and_its_terminator()
        {
            string usage = _writer.UsageFor(Request(FunctionKind.Return, "double", "string"));

            Assert.That(usage, Does.Contain("[Inject] private IFunctionProvider _functionProvider { get; set; }"));
            Assert.That(usage, Does.Contain("var calculateDamage = _functionProvider"));
            Assert.That(usage, Does.Contain(".Call<CalculateDamageFunction>()"));
            Assert.That(usage, Does.Contain(".AddParams(param0)"));
            Assert.That(usage, Does.Contain(".ExecuteAndGetResult<double>();"));
        }

        [Test]
        public void A_void_function_with_no_parameters_is_called_on_one_line()
        {
            string usage = _writer.UsageFor(Request(FunctionKind.Void));

            Assert.That(usage, Does.Contain("_functionProvider.Call<CalculateDamageFunction>().Execute();"));
            Assert.That(usage, Does.Not.Contain("AddParams"));
        }

        [Test]
        public void A_void_function_that_takes_parameters_passes_them()
        {
            string usage = _writer.UsageFor(Request(FunctionKind.Void, string.Empty, "string", "int"));

            Assert.That(usage, Does.Contain(".AddParams(param0, param1)"));
            Assert.That(usage, Does.Contain(".Execute();"));
        }

        /// <summary>
        /// An async function is reached with CallAsync, and the arity that carries a value is the
        /// one with a callback to hand in.
        /// </summary>
        [Test]
        public void An_async_function_that_answers_is_called_with_a_callback()
        {
            string usage = _writer.UsageFor(Request(FunctionKind.Async, "Profile"));

            Assert.That(usage, Does.Contain(".CallAsync<CalculateDamageFunction, Profile>()"));
            Assert.That(usage, Does.Contain(".AddFunctionCompletedCallback(OnCalculateDamageCompleted)"));
            Assert.That(usage, Does.Contain(".ExecuteAsync();"));
        }

        [Test]
        public void An_async_function_that_answers_nothing_is_called_on_one_line()
        {
            string usage = _writer.UsageFor(Request(FunctionKind.Async));

            Assert.That(usage, Does.Contain("_functionProvider.CallAsync<CalculateDamageFunction>().ExecuteAsync();"));
            Assert.That(usage, Does.Not.Contain("AddFunctionCompletedCallback"));
        }

        /// <summary>
        /// The names typed into the window are the ones the snippet passes, so it reads as the call
        /// the author has in mind rather than as a template to fill in afterwards.
        /// </summary>
        [Test]
        public void The_snippet_passes_the_parameter_names_that_were_typed()
        {
            FunctionScriptRequest request = Request(FunctionKind.Return, "double");
            request.Parameters.Add(new FunctionParameter {Type = "string", Name = "weaponId"});

            Assert.That(_writer.UsageFor(request), Does.Contain(".AddParams(weaponId)"));
        }

        [Test]
        public void The_file_is_written_into_the_namespace_it_was_given()
        {
            Assert.That(_writer.Write(Request(FunctionKind.Void)), Does.Contain("namespace Modules.Player.Controllers"));
        }

        /// <summary>
        /// Two injected types from one namespace import it once, and a type from the function's own
        /// namespace is not imported at all.
        /// </summary>
        [Test]
        public void A_namespace_is_imported_once_and_the_functions_own_is_not_imported()
        {
            FunctionScriptRequest request = Request(FunctionKind.Void);
            request.Injectables.Add(new FunctionInjectable {Type = "IPlayerModel", Namespace = "Modules.Player.Models"});
            request.Injectables.Add(new FunctionInjectable {Type = "IWeaponsModel", Namespace = "Modules.Player.Models"});
            request.Injectables.Add(new FunctionInjectable {Type = "IHudModel", Namespace = "Modules.Player.Controllers"});

            var imports = new List<string>(_writer.Write(request).Split('\n'));

            Assert.That(imports.FindAll(line => line.StartsWith("using Modules.Player.Models;")), Has.Count.EqualTo(1));
            Assert.That(imports.FindAll(line => line.StartsWith("using Modules.Player.Controllers;")), Is.Empty);
        }
    }
}