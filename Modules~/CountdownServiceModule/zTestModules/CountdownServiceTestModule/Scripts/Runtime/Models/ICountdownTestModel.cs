#if UNITY_EDITOR
namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Models
{
    public interface ICountdownTestModel
    {
        /// <summary>The id this test drives. One name, so start and stop mean the same countdown.</summary>
        string CountdownId { get; }

        /// <summary>How long the test countdown runs, in seconds.</summary>
        int Duration { get; }
    }
}
#endif
