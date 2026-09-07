#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.Editor.Console
{
    [Serializable]
    public class CompilerLogRecord
    {
        public string Message;
        public string File;
        public int Line;
        public bool IsError;
    }

    /// <summary>
    /// Compiler messages arrive during a compile and a domain reload follows, which wipes every
    /// static. They are parked as a string in SessionState and read back afterwards, so this
    /// turns the list into that string and back.
    /// </summary>
    public class CompilerLogStore
    {
        [Serializable]
        private class Wrapper
        {
            public List<CompilerLogRecord> Records = new();
        }

        public string Serialize(IReadOnlyList<CompilerLogRecord> records)
        {
            var wrapper = new Wrapper();

            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                    wrapper.Records.Add(records[i]);
            }

            return JsonUtility.ToJson(wrapper);
        }

        public List<CompilerLogRecord> Deserialize(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return new List<CompilerLogRecord>();

            try
            {
                var wrapper = JsonUtility.FromJson<Wrapper>(stored);
                return wrapper?.Records ?? new List<CompilerLogRecord>();
            }
            catch (Exception)
            {
                // SessionState is shared with the rest of the editor. A value this store did not
                // write is no compiler messages, never an exception mid-reload.
                return new List<CompilerLogRecord>();
            }
        }
    }
}
#endif
