using System.Reflection;
using FlowIoC.BaseModule.Provider.Update;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class UpdateProviderTests
    {
        [Test]
        public void LateUpdate_callbacks_run_after_the_cameras()
        {
            var attribute = typeof(UpdateProvider).GetCustomAttribute<DefaultExecutionOrder>();

            Assert.IsNotNull(attribute,
                "UpdateProvider carries no DefaultExecutionOrder, so its LateUpdate races CinemachineBrain "
                + "and anything placed over the world through AddLateUpdate is a frame behind the camera.");
            Assert.AreEqual(UpdateProvider.EXECUTION_ORDER, attribute.order);
            Assert.Greater(attribute.order, 0);
        }
    }
}
