using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Function;
using FlowIoC.BaseModule.Function.Provider;
using FlowIoC.BaseModule.Function.ReturnableFunctions;
using FlowIoC.BaseModule.Function.VoidFunctions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// What a Function does when a Command reaches for one: it runs where it stands, answers, and
    /// goes back to the pool unless it asked to stay.
    /// </summary>
    public class FunctionProviderTests
    {
        internal static readonly List<object> Instances = new();
        internal static int Runs;
        internal static bool RetainOnce;
        internal static FunctionBody LastRetained;

        private FunctionProvider _provider;

        [SetUp]
        public void SetUp()
        {
            Instances.Clear();
            Runs = 0;
            RetainOnce = false;
            LastRetained = null;

            StandInContext context = new StandInContext();
            _provider = new FunctionProvider {Context = context};

            // FunctionBody asks for IFunctionProvider, which a real Context binds in CoreBindings.
            // Without it every function reports a failed injection before it runs.
            context.InjectionBinder.BindInstance<IFunctionProvider>(_provider);
        }

        [Test]
        public void A_void_function_runs_where_it_is_called()
        {
            _provider.Execute<CountingFunction>().Run();

            Assert.That(Runs, Is.EqualTo(1));
        }

        [Test]
        public void A_returning_function_answers()
        {
            int answer = _provider.Execute<DoubleFunction>().AddParams(21).RunAndGetResult<int>();

            Assert.That(answer, Is.EqualTo(42));
        }

        [Test]
        public void A_function_takes_the_parameters_it_was_given()
        {
            string answer = _provider.Execute<JoinFunction>().AddParams("a", "b").RunAndGetResult<string>();

            Assert.That(answer, Is.EqualTo("ab"));
        }

        /// <summary>
        /// The parameters reach Execute through the function's own typed entry, so a mismatch is
        /// a report naming the function rather than a reflection exception from inside the provider.
        /// </summary>
        [Test]
        public void A_parameter_of_the_wrong_type_is_reported_and_the_function_does_not_run()
        {
            LogAssert.Expect(LogType.Error, new Regex("Execute parameter"));

            int answer = _provider.Execute<DoubleFunction>().AddParams("not a number").RunAndGetResult<int>();

            Assert.That(answer, Is.Zero);
        }

        [Test]
        public void A_function_given_fewer_parameters_than_it_takes_is_reported()
        {
            LogAssert.Expect(LogType.Error, new Regex("Execute signature mismatch"));

            int answer = _provider.Execute<DoubleFunction>().RunAndGetResult<int>();

            Assert.That(answer, Is.Zero);
        }

        [Test]
        public void A_function_is_pooled_and_handed_out_again()
        {
            _provider.Execute<CountingFunction>().Run();
            _provider.Execute<CountingFunction>().Run();

            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.That(Instances[0], Is.SameAs(Instances[1]),
                "the second call should have taken the first function back out of the pool");
        }

        /// <summary>
        /// A retained function stays out of the pool until it releases, so the next call has to
        /// build a second instance rather than hand out the one still in use.
        /// </summary>
        [Test]
        public void A_retained_function_is_not_handed_to_the_next_caller()
        {
            RetainOnce = true;
            _provider.Execute<CountingFunction>().Run();

            RetainOnce = false;
            _provider.Execute<CountingFunction>().Run();

            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.That(Instances[0], Is.Not.SameAs(Instances[1]),
                "the first was still retained, so it could not be reused");
        }

        [Test]
        public void A_released_function_goes_back_to_the_pool()
        {
            RetainOnce = true;
            _provider.Execute<CountingFunction>().Run();

            _provider.ReleaseFunctionManually(LastRetained);

            RetainOnce = false;
            _provider.Execute<CountingFunction>().Run();

            Assert.That(Instances[0], Is.SameAs(Instances[1]),
                "once released it is the instance the next call gets");
        }

        public class CountingFunction : FunctionVoid
        {
            public override void Execute()
            {
                Runs++;
                Instances.Add(this);

                if (!RetainOnce)
                    return;

                Retain();
                LastRetained = this;
            }
        }

        public class DoubleFunction : FunctionReturn<int, int>
        {
            public override int Execute(int value) => value * 2;
        }

        public class JoinFunction : FunctionReturn<string, string, string>
        {
            public override string Execute(string left, string right) => left + right;
        }
    }
}