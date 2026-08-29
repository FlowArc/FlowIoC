using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.CountdownServiceModule.Data.ValueObjects;
using Modules.CountdownServiceModule.Models;
using UnityEngine;

namespace Modules.CountdownServiceModule.Controllers
{
    /// <summary>
    /// What both tick commands need: the model, and the three readings taken off a countdown.
    /// </summary>
    internal abstract class TickProcessBaseCommand : Command
    {
        [Inject] protected ICountdownModel _countdownModel { get; set; }

        public override void Execute() { }

        protected int RemainingSeconds(CountdownVO countdown)
        {
            TimeSpan remaining = countdown.EndTime - _countdownModel.Time;

            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            return Mathf.RoundToInt((float) remaining.TotalSeconds);
        }

        protected int ElapsedSeconds(CountdownVO countdown)
        {
            return Mathf.RoundToInt((float) (_countdownModel.Time - countdown.InitialTime).TotalSeconds);
        }

        /// <summary>
        /// How much of the countdown is left, as 0..1. An entry with no duration is only
        /// measuring elapsed time, so there is no whole to take a fraction of and it reports 0
        /// rather than dividing by zero.
        /// </summary>
        protected float RemainingFraction(CountdownVO countdown, int remainingSeconds)
        {
            double total = countdown.Duration.TotalSeconds;

            return total <= 0 ? 0f : (float) (remainingSeconds / total);
        }
    }
}
