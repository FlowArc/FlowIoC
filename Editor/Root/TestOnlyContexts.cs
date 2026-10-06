#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FlowIoC.Editor.Modules;
using UnityEditor;

namespace FlowIoC.Editor.Root
{
    /// <summary>
    /// Which contexts are test code, read off where their script sits: a screen made for a test
    /// module lives inside it. The Root inspector badges such a context TEST SCREEN, and Add Sub
    /// Context offers it only to a Root whose own script is inside a test module - the kind of
    /// Root it was made for. Nothing is declared on the class; the folder is the one answer.
    ///
    /// The script is found by a search of the asset database, so each type is asked once and
    /// remembered for the life of the window or inspector that owns this.
    /// </summary>
    internal class TestOnlyContexts
    {
        private readonly Func<Type, string> _scriptPathOf;
        private readonly TestTreePath _testTree;
        private readonly Dictionary<Type, bool> _answers = new Dictionary<Type, bool>();

        internal TestOnlyContexts() : this(
            type => AssetDatabase.GetAssetPath(new ContextScriptResolver().For(type)), new TestTreePath())
        {
        }

        internal TestOnlyContexts(Func<Type, string> scriptPathOf, TestTreePath testTree)
        {
            _scriptPathOf = scriptPathOf;
            _testTree = testTree;
        }

        /// <summary>
        /// True when the context's script sits inside a test module. A context with no script -
        /// one from a precompiled assembly - is never taken for test code.
        /// </summary>
        internal bool IsTestOnly(Type contextType)
        {
            if (contextType == null) return false;

            if (!_answers.TryGetValue(contextType, out bool answer))
                _answers[contextType] = answer = _testTree.Contains(_scriptPathOf(contextType));

            return answer;
        }

        /// <summary>True when a Root's script, given by its asset path, sits inside a test module.</summary>
        internal bool IsTestRoot(string rootScriptPath) => _testTree.Contains(rootScriptPath);
    }
}
#endif
