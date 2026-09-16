#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.CodeGenerator.Menus.Module.ModuleGeneration
{
    /// <summary>
    /// Keeps the handoff across the domain reload. SessionState rather than EditorPrefs: it
    /// survives the reload, which is all the record has to survive, and it dies with the
    /// Editor session and belongs to this project. EditorPrefs is one file for every project on
    /// the machine, and a run that never finished in one project read as pending in the next.
    /// </summary>
    internal class ModuleGenerationHandoffStore
    {
        private const string KEY = "FlowIoC.CreateModule.Handoff";

        public void Write(ModuleGenerationHandoffEVO handoff)
        {
            SessionState.SetString(KEY, Serialize(handoff));
        }

        /// <summary>The pending run, or null when there is none.</summary>
        public ModuleGenerationHandoffEVO Read()
        {
            return Deserialize(SessionState.GetString(KEY, null));
        }

        public void Clear()
        {
            SessionState.EraseString(KEY);
        }

        public string Serialize(ModuleGenerationHandoffEVO handoff)
        {
            return JsonUtility.ToJson(handoff);
        }

        public ModuleGenerationHandoffEVO Deserialize(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return null;

            try
            {
                return JsonUtility.FromJson<ModuleGenerationHandoffEVO>(stored);
            }
            catch (Exception)
            {
                // SessionState is shared with the rest of the editor and outlives the code that
                // wrote it. A value this store did not write is no pending run, never an
                // exception in the middle of a domain reload.
                return null;
            }
        }
    }
}
#endif
