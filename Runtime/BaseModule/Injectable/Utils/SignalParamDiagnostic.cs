using System;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    internal enum SignalParamDiagnosticKind
    {
        /// <summary>The written index is past the last value of that type in the payload.</summary>
        IndexOutOfRange,

        /// <summary>Another property already took the slot this index points at.</summary>
        DuplicateClaim,

        /// <summary>No unclaimed value of this property's type is left in the payload.</summary>
        NoFreeSlot
    }

    /// <summary>
    /// A binding failure the resolver found. The resolver reports rather than logs, so it
    /// stays free of Unity dependencies and stays assertable from a plain unit test.
    /// </summary>
    internal readonly struct SignalParamDiagnostic
    {
        public readonly SignalParamDiagnosticKind Kind;
        public readonly Type TargetType;
        public readonly string PropertyName;
        public readonly Type PropertyType;
        public readonly int RequestedIndex;
        public readonly int CandidateCount;

        /// <summary>
        /// How many candidates of this property's type were already claimed. Meaningful
        /// only for <see cref="SignalParamDiagnosticKind.NoFreeSlot"/>; zero otherwise.
        /// </summary>
        public readonly int ClaimedCount;

        /// <summary>
        /// The property that already took the slot this one asked for. Set only for
        /// <see cref="SignalParamDiagnosticKind.DuplicateClaim"/>; null otherwise.
        /// </summary>
        public readonly string ClaimingPropertyName;

        public SignalParamDiagnostic(
            SignalParamDiagnosticKind kind,
            Type targetType,
            string propertyName,
            Type propertyType,
            int requestedIndex,
            int candidateCount,
            int claimedCount,
            string claimingPropertyName)
        {
            Kind = kind;
            TargetType = targetType;
            PropertyName = propertyName;
            PropertyType = propertyType;
            RequestedIndex = requestedIndex;
            CandidateCount = candidateCount;
            ClaimedCount = claimedCount;
            ClaimingPropertyName = claimingPropertyName;
        }
    }
}
