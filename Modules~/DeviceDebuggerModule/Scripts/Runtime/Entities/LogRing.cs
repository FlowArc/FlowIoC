using System.Collections.Generic;
using FlowIoC.ConsoleModule;
using Modules.DeviceDebuggerModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.DeviceDebuggerModule.Entities
{
    /// <summary>
    /// The rows the Console keeps: a fixed array that overwrites its oldest row past capacity,
    /// with the counts per kind kept in step, so the panel never walks the ring to count. An
    /// error kind - Error, Exception, Assert - raises UnreadErrors until the Console is shown.
    /// Version moves on every change, which is what the view compares before it repaints.
    /// </summary>
    public class LogRing
    {
        private readonly ConsoleLog[] _rows;
        private int _start;

        public LogRing(int capacity)
        {
            _rows = new ConsoleLog[capacity > 0 ? capacity : 1];
        }

        public int Capacity => _rows.Length;

        public int Count { get; private set; }

        public int Version { get; private set; }

        public int Logs { get; private set; }

        public int Warnings { get; private set; }

        public int Errors { get; private set; }

        public int UnreadErrors { get; private set; }

        /// <summary>Index 0 is the oldest row kept.</summary>
        public ConsoleLog this[int index] => _rows[(_start + index) % _rows.Length];

        public void Add(ConsoleLog log)
        {
            if (log == null) return;

            if (Count == _rows.Length)
            {
                Uncount(_rows[_start]);
                _start = (_start + 1) % _rows.Length;
                Count--;
            }

            _rows[(_start + Count) % _rows.Length] = log;
            Count++;
            CountRow(log);

            if (IsError(log)) UnreadErrors++;

            Version++;
        }

        public void MarkErrorsRead()
        {
            UnreadErrors = 0;
        }

        public void Clear()
        {
            for (int i = 0; i < _rows.Length; i++) _rows[i] = null;

            _start = 0;
            Count = 0;
            Logs = 0;
            Warnings = 0;
            Errors = 0;
            UnreadErrors = 0;
            Version++;
        }

        /// <summary>The rows the filter keeps, oldest first, into a list the caller owns.</summary>
        public void CopyTo(List<ConsoleLog> target, LogFilterVO filter)
        {
            target.Clear();

            for (int i = 0; i < Count; i++)
            {
                ConsoleLog row = this[i];

                if (filter == null || filter.Matches(row))
                    target.Add(row);
            }
        }

        public static bool IsError(ConsoleLog log) =>
            log.LogType != LogType.Log && log.LogType != LogType.Warning;

        private void CountRow(ConsoleLog log)
        {
            if (log.LogType == LogType.Log) Logs++;
            else if (log.LogType == LogType.Warning) Warnings++;
            else Errors++;
        }

        private void Uncount(ConsoleLog log)
        {
            if (log == null) return;

            if (log.LogType == LogType.Log) Logs--;
            else if (log.LogType == LogType.Warning) Warnings--;
            else Errors--;
        }
    }
}
