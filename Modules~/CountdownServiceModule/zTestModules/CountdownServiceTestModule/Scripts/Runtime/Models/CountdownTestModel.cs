#if UNITY_EDITOR
namespace Modules.CountdownServiceModule.CountdownServiceTestModule.Models
{
    public class CountdownTestModel : ICountdownTestModel
    {
        public string CountdownId => "TestCountdown";

        public int Duration => 60;
    }
}
#endif
