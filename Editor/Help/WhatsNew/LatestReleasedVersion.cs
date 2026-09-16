#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// The newest version of FlowIoC a registry has, asked for once a day and only while the
    /// What's New tab is being drawn - never on Editor start, so a project that nobody opens the
    /// Help window in makes no request at all. <see cref="LatestReleaseAskRule"/> says when the
    /// day is not waited out: a project updated to a release the last answer never heard of.
    ///
    /// The registry asked is the one the package was installed from, and OpenUPM, where FlowIoC
    /// is published, for a package that came in another way: a Git URL, a submodule, an embedded
    /// copy. That is what makes the line worth having on those roads, because on them the
    /// Package Manager itself never says a newer version exists.
    ///
    /// The answer is kept in EditorPrefs rather than in the project: which release is newest is a
    /// fact about the package, not about this project, and one developer's answer serves every
    /// project their Editor opens. A request that fails leaves the last answer where it was and
    /// still counts as today's ask, so a machine that is offline asks once and then reads.
    /// </summary>
    internal class LatestReleasedVersion
    {
        internal const string DEFAULT_REGISTRY_URL = "https://package.openupm.com";

        private const string VERSION_KEY = "FlowIoC.WhatsNew.LatestRelease";
        private const string ASKED_AT_KEY = "FlowIoC.WhatsNew.LatestReleaseAskedAt";
        private const string ASKED_WITH_KEY = "FlowIoC.WhatsNew.LatestReleaseAskedWith";
        private const int TIMEOUT_SECONDS = 10;

        private readonly string _url;
        private readonly string _installed;
        private readonly LatestReleaseAskRule _askRule = new LatestReleaseAskRule();
        private readonly LatestReleaseReading _reading = new LatestReleaseReading();
        private UnityWebRequest _request;

        internal LatestReleasedVersion() : this(new WhatsNewSource())
        {
        }

        internal LatestReleasedVersion(WhatsNewSource source)
        {
            string registry = string.IsNullOrEmpty(source.RegistryUrl) ? DEFAULT_REGISTRY_URL : source.RegistryUrl;

            _url = registry.TrimEnd('/') + "/" + source.PackageName;
            _installed = source.Version ?? string.Empty;
        }

        /// <summary>Where the ask goes, so a test can see the registry and the package it names.</summary>
        internal string Url => _url;

        /// <summary>
        /// The newest version known, or empty until a registry has answered once. When today's
        /// ask is still owed, it goes out now and <paramref name="onAnswered"/> is called when it
        /// lands, so that whoever drew the empty line can draw the real one.
        /// </summary>
        internal string Read(Action onAnswered)
        {
            if (IsAskOwed())
                Ask(onAnswered);

            return EditorPrefs.GetString(VERSION_KEY, string.Empty);
        }

        private bool IsAskOwed()
        {
            // A batch run has nobody reading and no window to draw in, and a request in flight is
            // this day's ask already.
            if (Application.isBatchMode || _request != null)
                return false;

            return _askRule.IsOwed(
                SinceLastAsk(),
                _installed,
                EditorPrefs.GetString(VERSION_KEY, string.Empty),
                EditorPrefs.GetString(ASKED_WITH_KEY, string.Empty));
        }

        private static TimeSpan? SinceLastAsk()
        {
            string askedAt = EditorPrefs.GetString(ASKED_AT_KEY, string.Empty);

            if (!long.TryParse(askedAt, out long ticks))
                return null;

            return DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
        }

        private void Ask(Action onAnswered)
        {
            _request = UnityWebRequest.Get(_url);
            _request.timeout = TIMEOUT_SECONDS;
            _request.SendWebRequest();

            EditorApplication.CallbackFunction poll = null;

            poll = () =>
            {
                if (!_request.isDone)
                    return;

                EditorApplication.update -= poll;

                Answered(_request);

                _request.Dispose();
                _request = null;

                onAnswered?.Invoke();
            };

            EditorApplication.update += poll;
        }

        /// <summary>
        /// The day's ask is spent whatever came back, so a registry that is down or has changed
        /// its format is asked again tomorrow, not on every draw. It is spent for the version
        /// installed now, so an install the registry has no release for asks once and not on
        /// every repaint. Only a version that reads is written, so a bad day never erases a good
        /// answer.
        /// </summary>
        private void Answered(UnityWebRequest request)
        {
            EditorPrefs.SetString(ASKED_AT_KEY, DateTime.UtcNow.Ticks.ToString());
            EditorPrefs.SetString(ASKED_WITH_KEY, _installed);

            if (request.result != UnityWebRequest.Result.Success)
                return;

            string latest = _reading.Of(request.downloadHandler.text);

            if (latest.Length > 0)
                EditorPrefs.SetString(VERSION_KEY, latest);
        }
    }
}

#endif