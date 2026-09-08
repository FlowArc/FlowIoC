using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Function;
using FlowIoC.BaseModule.Function.AsyncFunctions;
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
        internal static bool ReleaseInsideExecuteOnce;
        internal static bool CallSelfOnce;
        internal static FunctionBody LastRetained;

        private FunctionProvider _provider;

        [SetUp]
        public void SetUp()
        {
            Instances.Clear();
            Runs = 0;
            RetainOnce = false;
            ReleaseInsideExecuteOnce = false;
            CallSelfOnce = false;
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
            _provider.Call<CountingFunction>().Execute();

            Assert.That(Runs, Is.EqualTo(1));
        }

        [Test]
        public void A_returning_function_answers()
        {
            int answer = _provider.Call<DoubleFunction>().AddParams(21).ExecuteAndGetResult<int>();

            Assert.That(answer, Is.EqualTo(42));
        }

        [Test]
        public void A_function_takes_the_parameters_it_was_given()
        {
            string answer = _provider.Call<JoinFunction>().AddParams("a", "b").ExecuteAndGetResult<string>();

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

            int answer = _provider.Call<DoubleFunction>().AddParams("not a number").ExecuteAndGetResult<int>();

            Assert.That(answer, Is.Zero);
        }

        [Test]
        public void A_function_given_fewer_parameters_than_it_takes_is_reported()
        {
            LogAssert.Expect(LogType.Error, new Regex("Execute signature mismatch"));

            int answer = _provider.Call<DoubleFunction>().ExecuteAndGetResult<int>();

            Assert.That(answer, Is.Zero);
        }

        [Test]
        public void A_function_is_pooled_and_handed_out_again()
        {
            _provider.Call<CountingFunction>().Execute();
            _provider.Call<CountingFunction>().Execute();

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
            _provider.Call<CountingFunction>().Execute();

            RetainOnce = false;
            _provider.Call<CountingFunction>().Execute();

            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.That(Instances[0], Is.Not.SameAs(Instances[1]),
                "the first was still retained, so it could not be reused");
        }

        [Test]
        public void A_released_function_goes_back_to_the_pool()
        {
            RetainOnce = true;
            _provider.Call<CountingFunction>().Execute();

            _provider.ReleaseFunctionManually(LastRetained);

            RetainOnce = false;
            _provider.Call<CountingFunction>().Execute();

            Assert.That(Instances[0], Is.SameAs(Instances[1]),
                "once released it is the instance the next call gets");
        }

        /// <summary>
        /// A function that retains and releases inside its own Execute is back in the pool before
        /// that Execute returns. The run's own check then used to find nothing retained and pool it
        /// a second time, so one instance sat in the pool twice and two callers were handed it.
        /// </summary>
        [Test]
        public void A_function_released_inside_its_own_Execute_is_not_pooled_twice()
        {
            ReleaseInsideExecuteOnce = true;
            _provider.Call<CountingFunction>().Execute();

            ReleaseInsideExecuteOnce = false;
            _provider.Call<CountingFunction>().Execute();
            _provider.Call<CountingFunction>().Execute();

            Assert.That(Instances, Has.Count.EqualTo(3));
            Assert.That(Instances[1], Is.SameAs(Instances[0]), "the released instance is the one the pool had");
            Assert.That(Instances[2], Is.SameAs(Instances[0]),
                "and it was in the pool once, so the third call gets it back rather than a second copy");
        }

        /// <summary>
        /// The same thing in the shape that made it visible: a function that releases itself and
        /// then calls its own type reaches a pool holding that very instance. The outer run must not
        /// pool it on the way out - the nested run is the one holding it now.
        /// </summary>
        [Test]
        public void A_function_taken_out_again_mid_Execute_is_not_pooled_by_the_run_that_left_it()
        {
            ReleaseInsideExecuteOnce = true;
            CallSelfOnce = true;

            _provider.Call<CountingFunction>().Execute();

            ReleaseInsideExecuteOnce = false;
            CallSelfOnce = false;
            _provider.Call<CountingFunction>().Execute();
            _provider.Call<CountingFunction>().Execute();

            Assert.That(Instances[3], Is.SameAs(Instances[2]),
                "the pool held one instance, not the same one twice");
        }

        /// <summary>
        /// An async function is driven as a coroutine and never through the synchronous path, so
        /// calling one with Call would otherwise run nothing and report nothing. The provider has
        /// no reflection fallback to reach its Execute with - FunctionBody's constructor is
        /// internal, so the shipped arities are the only kinds there are - and this is what a
        /// caller gets instead of silence.
        /// </summary>
        [Test]
        public void An_async_function_called_with_Call_is_reported_and_does_not_run()
        {
            LogAssert.Expect(LogType.Error, new Regex("CallAsync"));

            _provider.Call<WaitingFunction>().Execute();

            Assert.That(Runs, Is.Zero);
        }

        public class WaitingFunction : AsyncFunction
        {
            public override IEnumerator Execute()
            {
                Runs++;
                yield break;
            }
        }

        public class CountingFunction : FunctionVoid
        {
            public override void Execute()
            {
                Runs++;
                Instances.Add(this);

                if (ReleaseInsideExecuteOnce)
                {
                    ReleaseInsideExecuteOnce = false;
                    Retain();
                    Release();

                    if (!CallSelfOnce)
                        return;

                    CallSelfOnce = false;
                    _functionProvider.Call<CountingFunction>().Execute();

                    return;
                }

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