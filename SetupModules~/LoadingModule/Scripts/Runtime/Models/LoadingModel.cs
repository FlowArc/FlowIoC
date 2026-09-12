using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.Data.UnityObjects;
using Modules.LoadingModule.Data.ValueObjects;
using Modules.LoadingModule.Enums;
using Modules.LoadingModule.RootsContexts;
using Modules.LoadingModule.Shared.Data.ValueObjects;
using Modules.LoadingModule.Shared.Enums;
using UnityEngine;

namespace Modules.LoadingModule.Models
{
    /// <summary>
    /// PostConstruct only takes CD_LoadingSets off the Root's adapter and builds the runtime sets
    /// from it; it decides nothing. Validation errors point at the asset, because the asset is what
    /// has to change.
    /// </summary>
    public class LoadingModel : ILoadingModel, IConstructable
    {
        [Inject(nameof(LoadingServiceContext))]
        private GameObject _root { get; set; }

        private readonly List<LoadingSetRVO> _sets = new();
        private readonly Dictionary<string, LoadingSetRVO> _setsByKey = new();
        private readonly Dictionary<string, LoadingStepRVO> _stepsByKey = new();
        private readonly Dictionary<string, List<LoadingStepRVO>> _parentsByChildSet = new();
        private readonly List<LoadingStepRVO> _noSteps = new();

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }

        public IReadOnlyList<LoadingSetRVO> Sets => _sets;
        public CD_LoadingSets Config { get; private set; }

        public void PostConstruct()
        {
            if (_root == null) return;

            var adapter = _root.GetComponent<RootAdapter>();
            if (adapter == null)
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    "LoadingServiceRoot has no RootAdapter, so CD_LoadingSets cannot be read and no set can run.", _root);
                return;
            }

            CD_LoadingSets config = adapter.GetScriptable<CD_LoadingSets>();
            if (config == null)
            {
                FlowLogger.LogError(FlowLogType.LoadingModule,
                    "LoadingServiceRoot's adapter files no CD_LoadingSets, so no set can run.", _root);
                return;
            }

            Load(config);
        }

        public void Deconstruct()
        {
            _sets.Clear();
            _setsByKey.Clear();
            _stepsByKey.Clear();
            _parentsByChildSet.Clear();
            Config = null;
        }

        public void Load(CD_LoadingSets config)
        {
            Deconstruct();
            Config = config;

            foreach (LoadingSetCVO setConfig in config.Sets)
            {
                if (string.IsNullOrEmpty(setConfig.Key))
                {
                    FlowLogger.LogError(FlowLogType.LoadingModule, "CD_LoadingSets has a set with no key.", config);
                    continue;
                }

                if (_setsByKey.ContainsKey(setConfig.Key))
                {
                    FlowLogger.LogError(FlowLogType.LoadingModule,
                        $"CD_LoadingSets declares the set '{setConfig.Key}' twice.", config);
                    continue;
                }

                var set = new LoadingSetRVO {Config = setConfig, State = LoadingSetState.NotBegun};
                _sets.Add(set);
                _setsByKey[setConfig.Key] = set;

                if (setConfig.Steps.Count == 0)
                    FlowLogger.LogWarning(FlowLogType.LoadingModule,
                        $"CD_LoadingSets set '{setConfig.Key}' has no steps, so it completes the moment it begins.");

                foreach (LoadingStepCVO stepConfig in setConfig.Steps)
                {
                    if (string.IsNullOrEmpty(stepConfig.Key))
                    {
                        FlowLogger.LogError(FlowLogType.LoadingModule,
                            $"CD_LoadingSets set '{setConfig.Key}' has a step with no key.", config);
                        continue;
                    }

                    if (_stepsByKey.TryGetValue(stepConfig.Key, out LoadingStepRVO taken))
                    {
                        FlowLogger.LogError(FlowLogType.LoadingModule,
                            $"CD_LoadingSets lists the step '{stepConfig.Key}' in both '{taken.Owner.Key}' and '{setConfig.Key}'. A step key belongs to one set.",
                            config);
                        continue;
                    }

                    var step = new LoadingStepRVO {Config = stepConfig, Owner = set, State = LoadingStepState.Pending};
                    set.Steps.Add(step);
                    _stepsByKey[stepConfig.Key] = step;
                }
            }

            // Second pass: a child set may be declared after the step that stands for it.
            foreach (LoadingStepRVO step in _stepsByKey.Values)
            {
                if (!step.Config.HasChild) continue;

                if (!_setsByKey.ContainsKey(step.Config.ChildSet))
                {
                    FlowLogger.LogError(FlowLogType.LoadingModule,
                        $"CD_LoadingSets step '{step.Config.Key}' stands for the set '{step.Config.ChildSet}', which is not declared.", config);
                    continue;
                }

                if (step.Config.ChildSet == step.Owner.Key)
                {
                    FlowLogger.LogError(FlowLogType.LoadingModule,
                        $"CD_LoadingSets step '{step.Config.Key}' stands for its own set '{step.Owner.Key}'.", config);
                    continue;
                }

                if (!_parentsByChildSet.TryGetValue(step.Config.ChildSet, out List<LoadingStepRVO> parents))
                    _parentsByChildSet[step.Config.ChildSet] = parents = new List<LoadingStepRVO>();

                parents.Add(step);
            }
        }

        public bool TryGetSet(string set, out LoadingSetRVO runtime) =>
            _setsByKey.TryGetValue(set ?? string.Empty, out runtime);

        public bool TryGetStep(string step, out LoadingStepRVO runtime) =>
            _stepsByKey.TryGetValue(step ?? string.Empty, out runtime);

        public IReadOnlyList<LoadingStepRVO> ParentStepsOf(string childSet) =>
            _parentsByChildSet.TryGetValue(childSet ?? string.Empty, out List<LoadingStepRVO> parents) ? parents : _noSteps;

        public void Begin(LoadingSetRVO set, float now)
        {
            set.State = LoadingSetState.Running;
            set.BeganAt = now;
            set.LastReportAt = now;
            set.EndedAt = 0f;
            set.FailedStep = null;
            set.LastTouchedStep = null;
            set.BeginAnnounced = false;
            set.StallWarned = false;

            // A resolved awaiter belongs to the run that ended; the next Await gets a fresh one.
            if (set.Awaiter != null && set.Awaiter.Task.IsCompleted)
                set.Awaiter = null;

            foreach (LoadingStepRVO step in set.Steps)
            {
                step.State = LoadingStepState.Pending;
                step.Fraction = 0f;
                step.Detail = null;
                step.StartedAt = 0f;
                step.EndedAt = 0f;
            }
        }

        public void Reopen(LoadingSetRVO set, float now)
        {
            set.State = LoadingSetState.Running;
            set.FailedStep = null;
            set.EndedAt = 0f;
            set.LastReportAt = now;
            set.StallWarned = false;

            if (set.Awaiter != null && set.Awaiter.Task.IsCompleted)
                set.Awaiter = null;
        }

        public void Apply(LoadingStepRVO step, LoadingStepReportVO report)
        {
            LoadingSetRVO set = step.Owner;
            set.LastReportAt = report.Time;
            set.StallWarned = false;
            set.LastTouchedStep = step;

            switch (report.Kind)
            {
                case LoadingReportKind.Start:
                    step.State = LoadingStepState.Running;
                    step.Fraction = 0f;
                    step.Detail = null;
                    step.StartedAt = report.Time;
                    step.EndedAt = 0f;
                    break;

                case LoadingReportKind.Progress:
                    StartIfPending(step, report.Time);
                    step.Fraction = Mathf.Clamp01(report.Fraction);
                    if (report.Detail != null) step.Detail = report.Detail;
                    break;

                case LoadingReportKind.Detail:
                    StartIfPending(step, report.Time);
                    step.Detail = report.Detail;
                    break;

                case LoadingReportKind.Complete:
                    step.State = LoadingStepState.Completed;
                    step.Fraction = 1f;
                    step.EndedAt = report.Time;
                    break;

                case LoadingReportKind.Skip:
                    step.State = LoadingStepState.Skipped;
                    step.Fraction = 1f;
                    step.EndedAt = report.Time;
                    break;

                case LoadingReportKind.Fail:
                    step.State = LoadingStepState.Failed;
                    step.EndedAt = report.Time;
                    if (!string.IsNullOrEmpty(report.Detail)) step.Detail = report.Detail;
                    set.FailedStep = step.Config.Key;
                    break;
            }
        }

        private static void StartIfPending(LoadingStepRVO step, float now)
        {
            if (step.State != LoadingStepState.Pending) return;

            step.State = LoadingStepState.Running;
            step.StartedAt = now;
        }

        public bool IsEnded(LoadingSetRVO set)
        {
            foreach (LoadingStepRVO step in set.Steps)
                if (!step.IsEnded)
                    return false;

            return true;
        }

        public void End(LoadingSetRVO set, float now)
        {
            set.State = set.FailedStep == null ? LoadingSetState.Completed : LoadingSetState.Failed;
            set.EndedAt = now;
            set.Awaiter?.TrySetResult(set.State == LoadingSetState.Completed);
        }

        public float Progress(LoadingSetRVO set)
        {
            float total = 0f;
            float done = 0f;

            foreach (LoadingStepRVO step in set.Steps)
            {
                float weight = Mathf.Max(0f, step.Config.Weight);
                total += weight;
                done += weight * Fraction(step);
            }

            if (total <= 0f)
                return IsEnded(set) ? 1f : 0f;

            return Mathf.Clamp01(done / total);
        }

        /// <summary>
        /// A step that stands for another set takes that set's progress while it runs and its own
        /// end state once it has ended - the step is completed by the child's completion, so once it
        /// has ended the child's number no longer matters. A failed step counts its full weight so the
        /// bar does not stop short of the end when the set reports failure.
        /// </summary>
        public float Fraction(LoadingStepRVO step)
        {
            if (step.IsEnded) return 1f;

            if (step.Config.HasChild && TryGetSet(step.Config.ChildSet, out LoadingSetRVO child))
                return child.State == LoadingSetState.NotBegun ? 0f : Progress(child);

            return step.State == LoadingStepState.Running ? step.Fraction : 0f;
        }

        public LoadingSetStatusRVO BuildStatus(LoadingSetRVO set)
        {
            var status = new LoadingSetStatusRVO
            {
                Set = set.Key,
                Presentation = set.Config.Presentation,
                State = set.State,
                Progress = Progress(set),
                FailedStep = set.FailedStep
            };

            // A running step of the set's own first, because it carries live detail; then a step whose
            // child set is running, whose message is the one the second bar sits under; then whatever
            // was touched last, so the text does not go blank between steps.
            LoadingStepRVO shown = MostRecentRunningStep(set) ?? RunningChildStep(set) ?? set.LastTouchedStep;
            if (shown != null)
            {
                status.Message = shown.Config.Message;
                status.Detail = shown.Detail;
            }

            LoadingStepRVO parent = RunningChildStep(set);
            if (parent != null && TryGetSet(parent.Config.ChildSet, out LoadingSetRVO child) && child.State == LoadingSetState.Running)
            {
                status.ChildSet = child.Key;
                status.ChildProgress = Progress(child);
                LoadingStepRVO childShown = MostRecentRunningStep(child) ?? child.LastTouchedStep;
                if (childShown != null)
                {
                    status.ChildMessage = childShown.Config.Message;
                    status.ChildDetail = childShown.Detail;
                }
            }

            return status;
        }

        private static LoadingStepRVO MostRecentRunningStep(LoadingSetRVO set)
        {
            LoadingStepRVO latest = null;
            foreach (LoadingStepRVO step in set.Steps)
            {
                if (step.State != LoadingStepState.Running) continue;
                if (latest == null || step.StartedAt >= latest.StartedAt) latest = step;
            }

            return latest;
        }

        private LoadingStepRVO RunningChildStep(LoadingSetRVO set)
        {
            foreach (LoadingStepRVO step in set.Steps)
            {
                if (!step.Config.HasChild || step.IsEnded) continue;
                if (TryGetSet(step.Config.ChildSet, out LoadingSetRVO child) && child.State == LoadingSetState.Running)
                    return step;
            }

            return null;
        }

        public TaskCompletionSource<bool> Awaiter(LoadingSetRVO set) =>
            set.Awaiter ??= new TaskCompletionSource<bool>();

        public bool TryTakeStallWarning(LoadingSetRVO set, float now, out string warning)
        {
            warning = null;

            if (set.State != LoadingSetState.Running || set.StallWarned) return false;
            if (now - set.LastReportAt < set.Config.StallWarningSeconds) return false;

            set.StallWarned = true;

            var notStarted = new StringBuilder();
            var running = new StringBuilder();
            foreach (LoadingStepRVO step in set.Steps)
            {
                if (step.State == LoadingStepState.Pending)
                    notStarted.Append(notStarted.Length == 0 ? "" : ", ").Append(step.Config.Key);
                else if (step.State == LoadingStepState.Running)
                    running.Append(running.Length == 0 ? "" : ", ").Append(step.Config.Key)
                        .Append(" (").Append(Mathf.RoundToInt(Fraction(step) * 100f)).Append("%)");
            }

            warning = $"'{set.Key}' has heard nothing for {Mathf.RoundToInt(now - set.LastReportAt)} s. "
                      + $"Not started: {(notStarted.Length == 0 ? "-" : notStarted.ToString())}. "
                      + $"Running: {(running.Length == 0 ? "-" : running.ToString())}. "
                      + "Is every Root whose steps CD_LoadingSets lists in the scene, and does every step end in Complete, Skip or Fail?";
            return true;
        }
    }
}