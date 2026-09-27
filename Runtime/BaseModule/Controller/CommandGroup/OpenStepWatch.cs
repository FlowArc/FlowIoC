using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace FlowIoC.BaseModule.Controller.CommandGroup
{
    /// <summary>
    /// The retained steps of the run that have not resolved yet, and a warning for each one that
    /// has waited longer than it should. A step reaches this only if it is still retained when its
    /// Execute returns, so a synchronous Command never touches it and the set is as small as the
    /// number of awaits in flight. It reports and nothing more: releasing or stopping is the
    /// Command's decision, and a watch that did it would hide the bug it exists to show.
    ///
    /// Everything that feeds it is conditional on the Editor or a Development Build, so a release
    /// build neither registers a step nor checks one.
    /// </summary>
    [HideInModelViewer]
    public class OpenStepWatch
    {
        public const double WARN_AFTER_SECONDS = 10;
        public const double CHECK_EVERY_SECONDS = 1;

        private const string InEditor = "UNITY_EDITOR";
        private const string InDevelopmentBuild = "DEVELOPMENT_BUILD";

        private sealed class OpenStep
        {
            public Type CommandType;
            public string RunName;
            public double OpenedAt;
            public double WarnAfter;
            public bool IsWarned;
        }

        private readonly Dictionary<ICommandBody, OpenStep> _open = new();
        private readonly Dictionary<Type, double> _thresholds = new();
        private readonly PooledCommandFrame _pooledFrame = new();
        private readonly Func<double> _clock;
        private readonly Action _tick;
        private double _nextCheckAt;
        private bool _hasReported;

        public OpenStepWatch() : this(() => Time.unscaledTimeAsDouble) { }

        internal OpenStepWatch(Func<double> clock)
        {
            _clock = clock;
            _tick = Tick;
        }

        /// <summary>How many retained steps are waiting right now. A test reads it to prove a flow left nothing hanging.</summary>
        public int OpenCount => _open.Count;

        /// <summary>Whether any step of this Command type is retained right now.</summary>
        internal bool IsOpen(Type commandType)
        {
            foreach (OpenStep step in _open.Values)
            {
                if (step.CommandType == commandType)
                    return true;
            }

            return false;
        }

        [Conditional(InEditor), Conditional(InDevelopmentBuild)]
        internal void Hook(IUpdateProvider updateProvider)
        {
            updateProvider?.AddUpdate(_tick);
            Application.quitting += ReportOpenSteps;
            Application.logMessageReceived += OnUnityLog;
        }

        [Conditional(InEditor), Conditional(InDevelopmentBuild)]
        internal void Open(ICommandBody command, string runName)
        {
            Type type = command.GetType();

            _open[command] = new OpenStep
            {
                CommandType = type,
                RunName = runName,
                OpenedAt = _clock(),
                WarnAfter = ThresholdOf(type)
            };
        }

        [Conditional(InEditor), Conditional(InDevelopmentBuild)]
        internal void Close(ICommandBody command)
        {
            _open.Remove(command);
        }

        private double ThresholdOf(Type type)
        {
            if (_thresholds.TryGetValue(type, out double seconds))
                return seconds;

            var longRetain = (LongRetainAttribute) Attribute.GetCustomAttribute(type, typeof(LongRetainAttribute));
            seconds = longRetain?.Seconds ?? WARN_AFTER_SECONDS;
            _thresholds[type] = seconds;
            return seconds;
        }

        private void Tick()
        {
            double now = _clock();
            if (now < _nextCheckAt) return;

            _nextCheckAt = now + CHECK_EVERY_SECONDS;
            Check();
        }

        [Conditional(InEditor), Conditional(InDevelopmentBuild)]
        internal void Check()
        {
            if (_open.Count == 0) return;

            double now = _clock();

            foreach (OpenStep step in _open.Values)
            {
                if (step.IsWarned || now - step.OpenedAt < step.WarnAfter)
                    continue;

                step.IsWarned = true;
                FlowLogger.LogWarning(SystemLogType.CommandOperation,
                    "'" + step.RunName + "' has waited " + (int) (now - step.OpenedAt) + " s on "
                    + step.CommandType.Name + ", which retained and has not called Release() or Stop(). "
                    + "Every path out of a retained Command ends in one of the two - an await has three. "
                    + "A step that waits this long on purpose carries [LongRetain].",
                    step.CommandType);
            }
        }

        /// <summary>
        /// A NullReferenceException thrown by a Command's code outside any running step, and no
        /// step of that Command retained: its instance was parked, which is why the reference is
        /// gone. One warning says so, blamed on the Command. Not conditional, for the same reason
        /// as <see cref="ReportOpenSteps"/>: Application.logMessageReceived needs a delegate to it.
        /// </summary>
        internal void OnUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || condition == null
                || !condition.StartsWith("NullReferenceException", StringComparison.Ordinal))
                return;

            Type command = _pooledFrame.Find(stackTrace);
            if (command == null || IsOpen(command))
                return;

            FlowLogger.LogWarning(SystemLogType.CommandOperation,
                command.Name + " was back in its pool when this ran - its step had already ended, and a "
                + "pooled Command holds none of its injections. Retain() the step and Release() it when "
                + "the wait is over.",
                command);
        }

        /// <summary>
        /// Play is ending: every step still over its threshold is listed in one warning, blamed on
        /// the first, so a hang that was scrolled past is named once more. A step under its
        /// threshold is left out - stopping Play in the middle of a download is not a hang. Written
        /// once and unsubscribed, so a watch of an earlier Play never reports into a later one.
        /// Not conditional itself, because Application.quitting needs a delegate to it; only the
        /// conditional Hook subscribes it, so a release build never calls it.
        /// </summary>
        internal void ReportOpenSteps()
        {
            Application.quitting -= ReportOpenSteps;
            Application.logMessageReceived -= OnUnityLog;
            if (_hasReported) return;
            _hasReported = true;

            double now = _clock();
            StringBuilder text = null;
            Type first = null;
            int count = 0;

            foreach (OpenStep step in _open.Values)
            {
                if (now - step.OpenedAt < step.WarnAfter)
                    continue;

                text ??= new StringBuilder();
                first ??= step.CommandType;
                count++;
                text.Append("\n  '").Append(step.RunName).Append("': ").Append(step.CommandType.Name)
                    .Append(", ").Append((int) (now - step.OpenedAt)).Append(" s");
            }

            if (count == 0) return;

            FlowLogger.LogWarning(SystemLogType.CommandOperation,
                "Play ended with " + count + " step(s) still retained:" + text, first);
        }
    }
}
