using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using FlowIoC.Editor.Console;
using NUnit.Framework;
using UnityEngine;

namespace FlowIoC.Tests
{
    public class FlowConsoleCollapseTests
    {
        private readonly FlowConsoleCollapse _collapse = new FlowConsoleCollapse();

        private static ConsoleLog Log(string message, int collapseKey)
        {
            return new ConsoleLog {Message = message, CollapseKey = collapseKey, LogType = LogType.Log};
        }

        [Test]
        public void Equal_rows_fold_into_one_with_a_count()
        {
            var rows = _collapse.Fold(new List<ConsoleLog> {Log("tick", 1), Log("tick", 1), Log("tick", 1)});

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(3, rows[0].Count);
            Assert.AreEqual("tick", rows[0].Log.Message);
        }

        [Test]
        public void Different_rows_stay_apart()
        {
            var rows = _collapse.Fold(new List<ConsoleLog> {Log("tick", 1), Log("tock", 2)});

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(1, rows[0].Count);
            Assert.AreEqual(1, rows[1].Count);
        }

        /// <summary>
        /// The first occurrence is what stays, so a row does not jump down the list every time
        /// its count goes up.
        /// </summary>
        [Test]
        public void A_folded_row_keeps_the_place_of_its_first_occurrence()
        {
            var first = Log("tick", 1);
            var rows = _collapse.Fold(new List<ConsoleLog> {first, Log("tock", 2), Log("tick", 1)});

            Assert.AreEqual(2, rows.Count);
            Assert.AreSame(first, rows[0].Log);
            Assert.AreEqual(2, rows[0].Count);
            Assert.AreEqual("tock", rows[1].Log.Message);
        }

        [Test]
        public void Nothing_folds_into_nothing()
        {
            Assert.AreEqual(0, _collapse.Fold(new List<ConsoleLog>()).Count);
            Assert.AreEqual(0, _collapse.Fold(null).Count);
        }
    }
}
