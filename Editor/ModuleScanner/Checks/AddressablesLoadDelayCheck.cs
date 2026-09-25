#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace FlowIoC.Editor.ModuleScanner
{
    /// <summary>
    /// Addressables' simulated load delay, which its settings default to 0.1 s. Under the Use Asset
    /// Database play mode a delay sends every load through DelayedActionManager, a hidden object
    /// that outlives Play; an instance left inactive by an earlier Play is found again by the next
    /// one, and a queue on an inactive object never runs. The boot then waits on the first screen
    /// for ever and nothing reaches the console.
    ///
    /// Any delay above zero is reported. Addressables 2.9 loads at once below a hundredth of a
    /// second, so a smaller delay does nothing today and costs nothing to report; the check does
    /// not rest on a number inside Addressables that a later version may move.
    ///
    /// Reported rather than written on its own: the settings asset is the project's, and a
    /// developer who wants the delay to watch a loading screen may keep it, with the row red.
    /// FlowIoC sets it to zero by itself only on settings it creates - see ScreenAddressables.
    /// </summary>
    internal class AddressablesLoadDelayCheck : IProjectCheck
    {
        private readonly Func<SettingsState> _state;
        private readonly Action<float> _write;

        internal AddressablesLoadDelayCheck() : this(DefaultState, DefaultWrite)
        {
        }

        internal AddressablesLoadDelayCheck(Func<SettingsState> state, Action<float> write)
        {
            _state = state;
            _write = write;
        }

        public string Id => "addressables-load-delay";

        public FindingEVO Inspect(ProjectTargetEVO project)
        {
            SettingsState state = _state();

            if (state.Delay == null || state.Delay <= 0f)
                return FindingEVO.Ok(Id, "Addressables load at once in the Editor");

            return FindingEVO.Fixable(Id,
                $"Addressables' simulated load delay is {state.Delay.Value.ToString(CultureInfo.InvariantCulture)} s - "
                + "a delay sends Editor loads through a queue an earlier Play can leave stopped, and the boot "
                + "hangs with nothing in the console. Fix sets it to 0",
                state.AssetPath);
        }

        public void Fix(ProjectTargetEVO project) => _write(0f);

        /// <summary>What one read of the settings found; a null delay means the project has no settings yet.</summary>
        internal class SettingsState
        {
            internal float? Delay { get; set; }
            internal string AssetPath { get; set; }
        }

        private static SettingsState DefaultState()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            return settings == null
                ? new SettingsState()
                : new SettingsState {Delay = settings.SimulatedLoadDelay, AssetPath = AssetDatabase.GetAssetPath(settings)};
        }

        private static void DefaultWrite(float delay)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null) return;

            settings.SimulatedLoadDelay = delay;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }
    }
}

#endif