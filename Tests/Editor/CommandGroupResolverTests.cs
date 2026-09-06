using System.Collections.Generic;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Controller.Binders;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Injectable.Binders;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Injectable.Utils;
using FlowIoC.BaseModule.Signals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlowIoC.Tests
{
    /// <summary>
    /// The command engine, driven without a scene. A stand-in context is enough because these
    /// commands ask for nothing: what is under test is the order steps run in, when a command goes
    /// back to the pool, and how the signal's payload reaches Execute.
    /// </summary>
    public class CommandGroupResolverTests
    {
        internal static readonly List<string> Steps = new();
        internal static readonly List<object> Instances = new();
        internal static int SeenNumber;
        internal static bool RetainOnce;
        internal static CommandBody LastRetained;
        internal static readonly List<CommandBody> Retained = new();

        private CommandBinder _commandBinder;

        [SetUp]
        public void SetUp()
        {
            Steps.Clear();
            Instances.Clear();
            SeenNumber = 0;
            RetainOnce = false;
            LastRetained = null;
            Retained.Clear();

            _commandBinder = new CommandBinder {Context = new StandInContext()};
        }

        #region Sequence and parallel

        [Test]
        public void A_sequence_runs_its_steps_in_the_order_they_were_bound()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<FirstCommand>()
                .ToSequence<SecondCommand>()
                .ToSequence<ThirdCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first", "second", "third"}));
        }

        [Test]
        public void Every_parallel_step_runs()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<FirstCommand>()
                .ToParallel<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Does.Contain("first"));
            Assert.That(Steps, Does.Contain("second"));
        }

        [Test]
        public void A_parallel_step_before_a_sequence_step_does_not_end_the_group()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<FirstCommand>()
                .ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first", "second"}));
        }

        [Test]
        public void A_sequence_step_before_a_parallel_step_does_not_end_the_group()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<FirstCommand>()
                .ToParallel<SecondCommand>()
                .ToParallel<ThirdCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first", "second", "third"}));
        }

        [Test]
        public void A_group_of_retained_parallel_steps_ends_when_the_last_one_releases()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<RetainingCommand>()
                .ToParallel<RetainingCommand>()
                .ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Retained, Has.Count.EqualTo(2), "both parallel steps started");

            Retained[0].Release();
            Retained[1].Release();

            Assert.That(Steps, Does.Contain("second"), "the step behind them ran once both had released");
        }

        [Test]
        public void A_second_dispatch_runs_the_sequence_again()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<FirstCommand>();

            signal.Dispatch();
            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first", "first"}));
        }

        #endregion

        #region Pooling

        [Test]
        public void A_command_is_pooled_and_handed_out_again()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<FirstCommand>();

            signal.Dispatch();
            signal.Dispatch();

            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.That(Instances[0], Is.SameAs(Instances[1]),
                "the second dispatch should have taken the first command back out of the pool");
        }

        #endregion

        #region Retain and release

        [Test]
        public void A_retained_command_holds_the_sequence_until_it_releases()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<RetainingCommand>()
                .ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"retaining"}), "the next step must wait");

            LastRetained.Release();

            Assert.That(Steps, Is.EqualTo(new[] {"retaining", "second"}), "and run once it is released");
        }

        /// <summary>
        /// The flag said "retained" for the life of the pooled instance, so a command that retains
        /// on one branch and not on another was treated as retained forever after its first retain -
        /// and the sequence behind it waited for a Release that was never coming.
        /// </summary>
        [Test]
        public void A_command_that_retained_once_is_not_taken_as_retained_the_next_time()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<ConditionallyRetainingCommand>()
                .ToSequence<SecondCommand>();

            RetainOnce = true;
            signal.Dispatch();
            LastRetained.Release();

            Steps.Clear();
            RetainOnce = false;
            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"conditional", "second"}),
                "the second run did not retain, so the sequence should have carried straight on");
        }

        #endregion

        #region Command groups

        [Test]
        public void A_group_step_runs_the_other_signal_s_chain_in_place()
        {
            Signal outer = new Signal(true);
            Signal inner = new Signal(true);

            _commandBinder.Bind(inner).ToSequence<SecondCommand>();
            _commandBinder.Bind(outer)
                .ToSequence<FirstCommand>()
                .ToGroupAsSequence(inner)
                .ToSequence<ThirdCommand>();

            outer.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first", "second", "third"}));
        }

        /// <summary>
        /// A group step used to return without counting itself started or finished, so the sequence
        /// waited on a sub-group that would never report and the resolver never went back to the
        /// pool. The step is skipped now, loudly, and the rest of the chain still runs.
        /// </summary>
        [Test]
        public void A_group_step_whose_signal_is_bound_nowhere_is_reported_and_skipped()
        {
            Signal outer = new Signal(true);
            Signal neverBound = new Signal(true);

            _commandBinder.Bind(outer)
                .ToGroupAsSequence(neverBound)
                .ToSequence<SecondCommand>();

            LogAssert.Expect(LogType.Error, new Regex("could not be found in any context"));

            outer.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"second"}), "the step behind the missing group still ran");
        }

        #endregion

        #region Payload

        /// <summary>
        /// Execute's parameters come from the binding, not from the signal. That is the split the
        /// framework is built on: the Context says what a step is given, and what the signal
        /// carried reaches the command through [SignalParam].
        /// </summary>
        [Test]
        public void Execute_is_handed_the_parameters_the_binding_gave_it()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<NumberCommand>(7);

            signal.Dispatch();

            Assert.That(SeenNumber, Is.EqualTo(7));
        }

        /// <summary>
        /// The other half of the same rule: what a retained command passes to Release is what the
        /// next step's Execute is handed. Between them, the binding and the previous Release are
        /// the only two things that fill Execute's parameters.
        /// </summary>
        [Test]
        public void Execute_is_handed_what_the_previous_command_released()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<ReleasingWithDataCommand>()
                .ToSequence<NumberCommand>();

            signal.Dispatch();
            LastRetained.Release(9);

            Assert.That(SeenNumber, Is.EqualTo(9));
        }

        [Test]
        public void A_signal_param_property_is_filled_from_the_payload()
        {
            Signal<int> signal = new Signal<int>(true);
            _commandBinder.Bind(signal).ToSequence<SignalParamCommand>();

            signal.Dispatch(7);

            Assert.That(SeenNumber, Is.EqualTo(7));
        }

        [Test]
        public void A_parameterless_Execute_ignores_a_payload_it_did_not_ask_for()
        {
            Signal<int> signal = new Signal<int>(true);
            _commandBinder.Bind(signal).ToSequence<FirstCommand>();

            signal.Dispatch(7);

            Assert.That(Steps, Is.EqualTo(new[] {"first"}));
        }

        [Test]
        public void A_parameter_of_the_wrong_type_is_reported_and_Execute_does_not_run()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<NumberCommand>("not a number");

            LogAssert.Expect(LogType.Error, new Regex("Execute parameter"));

            signal.Dispatch();

            Assert.That(SeenNumber, Is.Zero);
        }

        [Test]
        public void An_Execute_that_takes_more_than_the_binding_gave_it_is_reported()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<NumberCommand>();

            LogAssert.Expect(LogType.Error, new Regex("Execute signature mismatch"));

            signal.Dispatch();

            Assert.That(SeenNumber, Is.Zero);
        }

        #endregion

        #region Commands

        public class FirstCommand : Command
        {
            public override void Execute()
            {
                Steps.Add("first");
                Instances.Add(this);
            }
        }

        public class SecondCommand : Command
        {
            public override void Execute() => Steps.Add("second");
        }

        public class ThirdCommand : Command
        {
            public override void Execute() => Steps.Add("third");
        }

        public class RetainingCommand : Command
        {
            public override void Execute()
            {
                Retain();
                LastRetained = this;
                Retained.Add(this);
                Steps.Add("retaining");
            }
        }

        public class ConditionallyRetainingCommand : Command
        {
            public override void Execute()
            {
                Steps.Add("conditional");

                if (!RetainOnce)
                    return;

                Retain();
                LastRetained = this;
            }
        }

        public class ReleasingWithDataCommand : Command
        {
            public override void Execute()
            {
                Retain();
                LastRetained = this;
            }
        }

        public class NumberCommand : Command<int>
        {
            public override void Execute(int number) => SeenNumber = number;
        }

        public class SignalParamCommand : Command
        {
            [SignalParam] private int _number { get; set; }

            public override void Execute() => SeenNumber = _number;
        }

        #endregion

        /// <summary>
        /// Answers the few questions the resolver asks of a context and nothing else. A real Context
        /// would build providers and bind them across the run, which none of this needs.
        /// </summary>
        private class StandInContext : IContext
        {
            public List<IContext> SubContexts { get; set; } = new();
            public List<IContext> AllContexts { get; set; } = new();
            public bool IsTest { get; set; }
            public int InitializeOrder { get; set; }
            public bool IsStarted { get; set; }
            public MediationBinder MediationBinder { get; set; }
            public InjectionBinder InjectionBinder { get; set; }
            public InjectionBinderCrossContext InjectionBinderCrossContext { get; set; }
            public ICommandBinder CommandBinder { get; set; }

            private readonly SignalParamResolver _signalParamResolver = new();

            SignalParamResolver IContext.SignalParamResolver => _signalParamResolver;

            public void Initialize(GameObject contextGameObject, int initializeOrder,
                InjectionBinderCrossContext injectionBinderCrossContext, List<IContext> subContexts, bool isTest = false)
            {
            }

            public void Start()
            {
            }

            void IContext.InjectAllInstances()
            {
            }

            void IContext.ExecutePostConstructMethods()
            {
            }

            public void SignalBindings()
            {
            }

            public void InjectionBindings()
            {
            }

            public void MediationBindings()
            {
            }

            public void CommandBindings()
            {
            }

            public void Setup()
            {
            }

            public void Launch()
            {
            }

            public void DestroyContext()
            {
            }

            public void PauseContext()
            {
            }

            public void ResumeContext()
            {
            }
        }
    }
}