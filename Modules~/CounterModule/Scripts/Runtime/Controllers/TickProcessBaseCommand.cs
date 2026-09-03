using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CounterModule.Data.ValueObjects;
using Modules.CounterModule.Models;
using UnityEngine;

namespace Modules.CounterModule.Controllers
{
    /// <summary>
    /// What both tick commands need: the model, and the three readings taken off a counter.
    /// </summary>
    internal abstract class TickProcessBaseCommand : Command
    {
        [Inject] protected ICounterModel _counterModel { get; set; }

        public override void Execute() { }

        protected int RemainingSeconds(CounterVO counter)
        {
            TimeSpan remaining = counter.EndTime - _counterModel.Time;

            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            return Mathf.RoundToInt((float) remaining.TotalSeconds);
        }

        protected int ElapsedSeconds(CounterVO counter)
        {
            return Mathf.RoundToInt((float) (_counterModel.Time - counter.InitialTime).TotalSeconds);
        }

        /// <summary>
        /// How much of the countdown is left, as 0..1. An entry with no duration is only
        /// measuring elapsed time, so there is no whole to take a fraction of and it reports 0
        /// rather than dividing by zero.
        /// </summary>
        protected float RemainingFraction(CounterVO counter, int remainingSeconds)
        {
            double total = counter.Duration.TotalSeconds;

            return total <= 0 ? 0f : (float) (remainingSeconds / total);
        }
    }
}
