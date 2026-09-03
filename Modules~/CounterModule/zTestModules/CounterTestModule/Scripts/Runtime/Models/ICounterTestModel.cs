#if UNITY_EDITOR
namespace Modules.CounterModule.CounterTestModule.Models
{
    public interface ICounterTestModel
    {
        /// <summary>The id this test drives. One name, so start and stop mean the same counter.</summary>
        string CounterId { get; }

        /// <summary>How long the test counter runs, in seconds.</summary>
        int Duration { get; }
    }
}
#endif
