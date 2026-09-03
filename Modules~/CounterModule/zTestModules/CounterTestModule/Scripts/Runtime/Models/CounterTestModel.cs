#if UNITY_EDITOR
namespace Modules.CounterModule.CounterTestModule.Models
{
    public class CounterTestModel : ICounterTestModel
    {
        public string CounterId => "TestCounter";

        public int Duration => 60;
    }
}
#endif
