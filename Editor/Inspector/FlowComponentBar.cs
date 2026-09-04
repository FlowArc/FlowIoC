#if UNITY_EDITOR

using System;
using FlowIoC.BaseModule.Attributes;

namespace FlowIoC.Editor.Inspector
{
    /// <summary>
    /// The header bar, for a component that already has an editor of its own. A dedicated editor
    /// beats both Odin and the catch-all, so a component like the screen manager would otherwise
    /// be the one thing in the scene without a bar; this is the one line its editor adds.
    /// </summary>
    internal class FlowComponentBar
    {
        private readonly FlowPalette _palette = new FlowPalette();
        private readonly FlowRoleResolver _roles = new FlowRoleResolver();
        private readonly FlowHelpSource _help = new FlowHelpSource(new MonoScriptText());
        private readonly FlowHelpState _state = new FlowHelpState();

        private readonly FlowHeaderBar _bar;

        public FlowComponentBar()
        {
            _bar = new FlowHeaderBar(_palette, new FlowHelpPageMap());
        }

        public void Draw(Type type)
        {
            if (type == null || !_roles.TryResolve(type, out FlowRole role))
                return;

            bool open = _state.IsOpen(type, FlowHelpParser.TypeKey);

            _bar.Draw(role, _roles.TitleFor(type), type.Assembly.GetName().Name, _roles.LabelFor(type, role),
                _help.Summary(type), open,
                () => _state.SetOpen(type, FlowHelpParser.TypeKey, !open));
        }
    }
}

#endif
