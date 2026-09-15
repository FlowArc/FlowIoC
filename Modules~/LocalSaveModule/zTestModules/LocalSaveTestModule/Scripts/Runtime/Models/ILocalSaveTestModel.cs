#if UNITY_EDITOR

namespace Modules.LocalSaveModule.LocalSaveTestModule.Models
{
    public interface ILocalSaveTestModel
    {
        int Counter { get; }
        void Increment();
    }
}

#endif
