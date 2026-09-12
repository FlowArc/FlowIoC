using Modules.LoadingModule.Enums;

namespace Modules.LoadingModule.Data.ValueObjects
{
    /// <summary>One step's runtime state. Owner is the set it is declared in.</summary>
    public class LoadingStepRVO
    {
        public LoadingStepCVO Config;
        public LoadingSetRVO Owner;
        public LoadingStepState State;
        public float Fraction;
        public string Detail;
        public float StartedAt;
        public float EndedAt;

        public bool IsEnded => State == LoadingStepState.Completed
                               || State == LoadingStepState.Skipped
                               || State == LoadingStepState.Failed;
    }
}
