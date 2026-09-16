#if UNITY_EDITOR
using System;

namespace FlowIoC.Editor.Help.WhatsNew
{
    /// <summary>
    /// When the registry is asked again for the newest release. Once a day is the rule. The one
    /// thing that does not wait for the day is an install newer than the newest release the last
    /// answer named: the developer has a release that answer has never heard of, which is proof
    /// enough that it is stale. That ask is spent for the version it was made from, so a
    /// maintainer's checkout - ahead of every release, and told so by the registry - asks once and
    /// then reads like everybody else, and an offline machine asks once whatever it has installed.
    /// </summary>
    internal class LatestReleaseAskRule
    {
        internal static readonly TimeSpan ASK_AGAIN_AFTER = TimeSpan.FromDays(1);

        private readonly VersionOrder _order = new VersionOrder();

        /// <param name="sinceLastAsk">How long ago the registry was last asked; null when it never was.</param>
        /// <param name="installed">The version this project has.</param>
        /// <param name="known">The newest release the last answer named; empty until one has.</param>
        /// <param name="askedWith">The version that was installed when the registry was last asked.</param>
        internal bool IsOwed(TimeSpan? sinceLastAsk, string installed, string known, string askedWith)
        {
            if (sinceLastAsk == null || sinceLastAsk.Value >= ASK_AGAIN_AFTER)
                return true;

            return _order.IsNewer(installed, known) && installed != askedWith;
        }
    }
}

#endif
