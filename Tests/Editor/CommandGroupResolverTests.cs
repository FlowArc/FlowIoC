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
        internal static bool ReleaseInsideExecuteOnce;
        internal static CommandBody LastRetained;
        internal static readonly List<CommandBody> Retained = new();
        internal static object SeenModel;

        private CommandBinder _commandBinder;

        [SetUp]
        public void SetUp()
        {
            Steps.Clear();
            Instances.Clear();
            SeenNumber = 0;
            RetainOnce = false;
            ReleaseInsideExecuteOnce = false;
            LastRetained = null;
            Retained.Clear();
            SeenModel = null;
            DispatchingCommand.Target = null;

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

        /// <summary>
        /// A command that retains and releases inside its own Execute hands its instance back while
        /// its frame is still on the stack. If the next step is the same type it takes that very
        /// instance out of the pool, and when the outer frame resumes it must not read the nested
        /// run's flags as its own - it used to, and returned the instance to the pool a second time.
        /// </summary>
        [Test]
        public void A_command_released_inside_Execute_is_not_returned_twice_when_the_same_type_follows()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<ReleasingInsideExecuteCommand>()
                .ToSequence<ReleasingInsideExecuteCommand>()
                .ToSequence<SecondCommand>();

            ReleaseInsideExecuteOnce = true;
            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"releasing", "releasing", "second"}));
            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.That(Instances[0], Is.SameAs(Instances[1]),
                "the second step reused the instance the first had just released");
        }

        /// <summary>
        /// Stop ends the group while another step may still be holding its command for work that
        /// has not come back yet. Parking that command would hand it to the next dispatch while the
        /// old work still holds it, so the group lets go of it instead and the next dispatch builds
        /// a fresh one.
        /// </summary>
        [Test]
        public void A_command_still_retained_when_its_group_stops_is_not_handed_to_the_next_dispatch()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<RetainingCommand>()
                .ToSequence<StoppingCommand>();

            signal.Dispatch();
            CommandBody stillWorking = Retained[0];

            Signal again = new Signal(true);
            _commandBinder.Bind(again).ToSequence<RetainingCommand>();
            again.Dispatch();

            Assert.That(Retained, Has.Count.EqualTo(2));
            Assert.That(Retained[1], Is.Not.SameAs(stillWorking),
                "the instance the stopped group let go of is still busy and must not be reused");
        }

        /// <summary>
        /// A pooled command asks for the same models on every run, so its members are resolved once
        /// and remembered against the run's binding generation. The generation is what makes that
        /// safe: a model rebound - a scene coming back, a module's Root built again - has to reach
        /// the instance that is already in the pool, and it does because the generation moved.
        /// </summary>
        [Test]
        public void A_pooled_command_resolves_its_members_again_after_a_binding_changed()
        {
            StandInContext context = new StandInContext {InjectionBinderCrossContext = new InjectionBinderCrossContext()};
            CommandBinder binder = new CommandBinder {Context = context};

            Model first = context.InjectionBinder.Bind<IModel, Model>() as Model;

            Signal signal = new Signal(true);
            binder.Bind(signal).ToSequence<ModelReadingCommand>();

            signal.Dispatch();
            Assert.AreSame(first, SeenModel, "the first run was handed what was bound");

            context.InjectionBinder.UnBind<IModel>(first);
            IModel second = context.InjectionBinder.Bind<IModel, Model>();

            signal.Dispatch();

            Assert.That(Instances, Has.Count.EqualTo(2));
            Assert.AreSame(Instances[0], Instances[1], "the same pooled instance ran twice");
            Assert.AreSame(second, SeenModel, "and it was handed the model that replaced the first");
        }

        #endregion

        #region Stop

        /// <summary>
        /// Stop on a parallel step ends that step and nothing else. The steps beside it are still
        /// running and still hold their commands, so the group has to stay open for them - which is
        /// the whole difference between stopping a parallel step and stopping a sequence one.
        /// </summary>
        [Test]
        public void Stopping_one_parallel_step_leaves_the_group_waiting_for_the_others()
        {
            Signal outer = new Signal(true);
            Signal inner = new Signal(true);

            _commandBinder.Bind(inner)
                .ToParallel<RetainingCommand>()
                .ToParallel<RetainingCommand>();
            _commandBinder.Bind(outer)
                .ToGroupAsSequence(inner)
                .ToSequence<ThirdCommand>();

            outer.Dispatch();

            Assert.That(Retained, Has.Count.EqualTo(2), "both parallel steps started");

            Retained[0].Stop();

            Assert.That(Steps, Does.Not.Contain("third"), "the other parallel step is still running");

            Retained[1].Release();

            Assert.That(Steps, Does.Contain("third"), "the group ended once the last one finished");
        }

        /// <summary>
        /// The other half: stopping the one parallel step still running is what ends the group,
        /// because nothing is left to wait for and there is nothing left to start.
        /// </summary>
        [Test]
        public void Stopping_the_last_parallel_step_still_running_ends_the_group()
        {
            Signal outer = new Signal(true);
            Signal inner = new Signal(true);

            _commandBinder.Bind(inner)
                .ToParallel<RetainingCommand>()
                .ToParallel<RetainingCommand>();
            _commandBinder.Bind(outer)
                .ToGroupAsSequence(inner)
                .ToSequence<ThirdCommand>();

            outer.Dispatch();
            Retained[0].Release();

            Assert.That(Steps, Does.Not.Contain("third"), "one parallel step is still running");

            Retained[1].Stop();

            Assert.That(Steps, Does.Contain("third"));
        }

        /// <summary>
        /// Stop on a sequence step ends the whole group, because the steps behind it were waiting
        /// on this one and there is now nothing coming. The group that contains it carries on: a
        /// stopped group is a finished group as far as its parent is concerned.
        /// </summary>
        [Test]
        public void Stopping_a_sequence_step_drops_the_steps_behind_it_and_the_parent_carries_on()
        {
            Signal outer = new Signal(true);
            Signal inner = new Signal(true);

            _commandBinder.Bind(inner)
                .ToSequence<StoppingCommand>()
                .ToSequence<SecondCommand>();
            _commandBinder.Bind(outer)
                .ToGroupAsSequence(inner)
                .ToSequence<ThirdCommand>();

            outer.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"stopping", "third"}),
                "the step behind the stopped one never ran, and the parent's did");
        }

        /// <summary>
        /// Stop is for a command that is holding its group open, so a command that never retained
        /// has nothing to stop. It is reported rather than obeyed, and the step is judged the way
        /// any synchronous step is - which is what keeps the sequence running.
        /// </summary>
        [Test]
        public void A_stop_from_a_command_that_did_not_retain_is_refused()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToSequence<StoppingWithoutRetainCommand>()
                .ToSequence<SecondCommand>();

            LogAssert.Expect(LogType.Error, new Regex("must be retained to call STOP"));

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"stopping-without-retain", "second"}));
        }

        /// <summary>
        /// A group that stops lets go of the commands still working for it rather than pooling them,
        /// so the work they are waiting on eventually calls Release into a group that is over. The
        /// empty dictionary is what tells that Release its group is gone, and the instance must not
        /// reach the pool through it: two dispatches holding one command is the bug this guards.
        /// The warning that goes with it is a FlowLogger warning, which compiles out without
        /// ENABLE_LOG, so what is asserted here is the effect rather than the message.
        /// </summary>
        [Test]
        public void A_release_that_arrives_after_its_group_stopped_changes_nothing()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<RetainingCommand>()
                .ToSequence<StoppingCommand>();

            signal.Dispatch();
            CommandBody stillWorking = Retained[0];

            stillWorking.Release();

            Signal again = new Signal(true);
            _commandBinder.Bind(again).ToSequence<RetainingCommand>();
            again.Dispatch();

            Assert.That(Retained, Has.Count.EqualTo(2));
            Assert.That(Retained[1], Is.Not.SameAs(stillWorking),
                "the late Release must not have handed the busy instance back to the pool");
        }

        /// <summary>
        /// The same door from the other side. Stop arriving after the group ended is the case where
        /// the work came back and gave up rather than finishing, and it is refused for the same
        /// reason a late Release is.
        /// </summary>
        [Test]
        public void A_stop_that_arrives_after_its_group_stopped_changes_nothing()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal)
                .ToParallel<RetainingCommand>()
                .ToSequence<StoppingCommand>();

            signal.Dispatch();
            CommandBody stillWorking = Retained[0];

            stillWorking.Stop();

            Signal again = new Signal(true);
            _commandBinder.Bind(again).ToSequence<RetainingCommand>();
            again.Dispatch();

            Assert.That(Retained, Has.Count.EqualTo(2));
            Assert.That(Retained[1], Is.Not.SameAs(stillWorking));
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

            LogAssert.Expect(LogType.Error, new Regex("is not bound in"));

            outer.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"second"}), "the step behind the missing group still ran");
        }

        /// <summary>
        /// A sub group finishing puts its resolver back in the pool and the parent carries straight
        /// on - so a step of the parent that dispatches a signal takes that same resolver out
        /// again. The finished run's frames are still on the stack underneath it, and they used to
        /// dispose the run that had just taken it: the retained command went back to the pool
        /// mid-flight and the step behind it never ran.
        /// </summary>
        [Test]
        public void A_resolver_taken_out_of_the_pool_again_is_not_closed_by_the_run_that_left_it()
        {
            Signal outer = new Signal(true);
            Signal sub = new Signal(true);
            Signal late = new Signal(true);

            DispatchingCommand.Target = late;

            _commandBinder.Bind(sub).ToSequence<SecondCommand>();
            _commandBinder.Bind(late)
                .ToSequence<RetainingCommand>()
                .ToSequence<ThirdCommand>();
            _commandBinder.Bind(outer)
                .ToGroupAsSequence(sub)
                .ToSequence<DispatchingCommand>();

            outer.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"second", "dispatching", "retaining"}));

            LastRetained.Release();

            Assert.That(Steps, Is.EqualTo(new[] {"second", "dispatching", "retaining", "third"}),
                "the step behind the retained command still had its group");
        }

        #endregion

        #region Signal ownership

        /// <summary>
        /// A signal carries one command callback, and the binder that put it there gives it back
        /// when it is unbound. Without that the signal still points at a torn-down context:
        /// reloading a scene rebuilds every context while the signal holder, which lives in the
        /// cross-context binder, is the same instance - and the new binder was then refused on
        /// behalf of a run that was already over.
        /// </summary>
        [Test]
        public void A_signal_can_be_bound_again_after_the_binder_that_owned_it_unbound_everything()
        {
            Signal signal = new Signal(true);

            CommandBinder gone = new CommandBinder {Context = new StandInContext()};
            gone.Bind(signal).ToSequence<FirstCommand>();
            gone.UnBindAll();

            _commandBinder.Bind(signal).ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"second"}));
        }

        [Test]
        public void A_signal_unbound_by_key_can_be_bound_again()
        {
            Signal signal = new Signal(true);

            CommandBinder gone = new CommandBinder {Context = new StandInContext()};
            gone.Bind(signal).ToSequence<FirstCommand>();
            gone.UnBind(signal);

            _commandBinder.Bind(signal).ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"second"}));
        }

        /// <summary>
        /// The guard it was written for still holds: two binders alive at once do not share a
        /// signal, and the one that had it keeps it.
        /// </summary>
        [Test]
        public void A_signal_bound_in_a_live_binder_is_refused_a_second_owner()
        {
            Signal signal = new Signal(true);

            CommandBinder owner = new CommandBinder {Context = new StandInContext()};
            owner.Bind(signal).ToSequence<FirstCommand>();

            LogAssert.Expect(LogType.Error, new Regex("already bound to commands"));

            _commandBinder.Bind(signal).ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first"}));
        }

        #endregion

        /// <summary>
        /// The same signal bound twice in one Context is a mistake, and it used to be reported by a
        /// null reference at the second chain's first ToSequence. It is reported by name now, the
        /// second chain is accepted so the line still reads, and only the first chain runs.
        /// </summary>
        [Test]
        public void Binding_a_signal_twice_in_one_context_is_reported_and_only_the_first_chain_runs()
        {
            Signal signal = new Signal(true);
            _commandBinder.Bind(signal).ToSequence<FirstCommand>();

            LogAssert.Expect(LogType.Error, new Regex("bound twice"));

            _commandBinder.Bind(signal).ToSequence<SecondCommand>();

            signal.Dispatch();

            Assert.That(Steps, Is.EqualTo(new[] {"first"}));
        }

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

        /// <summary>Dispatches another signal from inside its own Execute, the way a step that
        /// hands work to another module does.</summary>
        public class DispatchingCommand : Command
        {
            internal static Signal Target;

            public override void Execute()
            {
                Steps.Add("dispatching");
                Target?.Dispatch();
            }
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

        /// <summary>Retains and releases inside its own Execute on the first run, and does neither on the next.</summary>
        public class ReleasingInsideExecuteCommand : Command
        {
            public override void Execute()
            {
                Steps.Add("releasing");
                Instances.Add(this);

                if (!ReleaseInsideExecuteOnce)
                    return;

                ReleaseInsideExecuteOnce = false;
                Retain();
                Release();
            }
        }

        /// <summary>Ends its group from inside its own Execute, the way a step that finds nothing to do does.</summary>
        public class StoppingCommand : Command
        {
            public override void Execute()
            {
                Steps.Add("stopping");
                Retain();
                Stop();
            }
        }

        /// <summary>Asks to stop without having retained, which is the one thing Stop is for.</summary>
        public class StoppingWithoutRetainCommand : Command
        {
            public override void Execute()
            {
                Steps.Add("stopping-without-retain");
                Stop();
            }
        }

        internal interface IModel
        {
        }

        internal class Model : IModel
        {
        }

        /// <summary>Records the model it was handed, so a second run can be asked whether it was handed the same one.</summary>
        public class ModelReadingCommand : Command
        {
            [Inject] private IModel _model { get; set; }

            public override void Execute()
            {
                Instances.Add(this);
                SeenModel = _model;
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
    }
}