using FlowIoC.BaseModule.Signals;
using FlowIoC.AssetModule.Signals;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class SignalHolderOriginTests
    {
        private readonly SignalHolderOrigin _origin = new SignalHolderOrigin();

        private class GameSignals : ISignalHolder
        {
            public readonly GameSignalsIncoming Incoming = new();
            public readonly Signal Loose = new();
        }

        private class GameSignalsIncoming
        {
            public readonly Signal Start = new();
            public readonly Signal<int> AddScore = new();
        }

        [Test]
        public void A_holder_the_framework_declares_is_the_frameworks()
        {
            Assert.IsTrue(_origin.IsFrameworkHolder(typeof(AssetSignals)));
        }

        [Test]
        public void A_holder_a_game_declares_is_not()
        {
            Assert.IsFalse(_origin.IsFrameworkHolder(typeof(GameSignals)));
            Assert.IsFalse(_origin.IsFrameworkHolder(null));
        }

        /// <summary>
        /// The halves are where a holder keeps its signals, so a stamp that only walked the top
        /// level would mark none of the ones that matter.
        /// </summary>
        [Test]
        public void Stamping_reaches_the_signals_inside_Incoming_and_Outgoing()
        {
            var holder = new GameSignals();

            ((ISignalBody) holder.Incoming.Start).IsFrameworkOwned = true;
            ((ISignalBody) holder.Incoming.AddScore).IsFrameworkOwned = true;
            ((ISignalBody) holder.Loose).IsFrameworkOwned = true;

            _origin.Stamp(holder);

            Assert.IsFalse(((ISignalBody) holder.Incoming.Start).IsFrameworkOwned);
            Assert.IsFalse(((ISignalBody) holder.Incoming.AddScore).IsFrameworkOwned);
            Assert.IsFalse(((ISignalBody) holder.Loose).IsFrameworkOwned);
        }

        [Test]
        public void Nothing_to_stamp_is_not_a_failure()
        {
            _origin.Stamp(null);
        }
    }
}
