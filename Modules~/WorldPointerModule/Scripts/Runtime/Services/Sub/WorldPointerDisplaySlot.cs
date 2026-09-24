using System;
using Modules.WorldPointerModule.Data.ValueObjects;
using Object = UnityEngine.Object;

namespace Modules.WorldPointerModule.Services.Sub
{
    /// <summary>
    /// One registered display with its content type closed over, so the service can keep displays
    /// of every type in one table and hand content on as object. The casts live here and nowhere
    /// else; RegisterDisplay and SetContent check the type before anything reaches them.
    /// </summary>
    internal abstract class WorldPointerDisplaySlot
    {
        public abstract IWorldPointerDisplay Display { get; }

        public abstract Type ContentType { get; }

        public WorldPointerOptionsCVO Options => Display.Options ?? _defaults;

        /// <summary>A display that is a Unity object and was destroyed - a screen unloaded without unregistering.</summary>
        public bool IsDestroyed => Display is Object unityObject && unityObject == null;

        /// <summary>The component and the GameObject it sits on, or the type name - for errors and RD_WorldPointer.</summary>
        public string Name => Display is UnityEngine.Component component && component != null
            ? $"{component.GetType().Name} on {component.gameObject.name}"
            : Display.GetType().Name;

        private readonly WorldPointerOptionsCVO _defaults = new();

        public bool Accepts(object content) => content == null || ContentType.IsInstanceOfType(content);

        public abstract IWorldPointerIndicator Acquire();

        public abstract void Release(IWorldPointerIndicator indicator);

        public abstract void SetContent(IWorldPointerIndicator indicator, object content);
    }

    internal sealed class WorldPointerDisplaySlot<TContent> : WorldPointerDisplaySlot
    {
        private readonly IWorldPointerDisplay<TContent> _display;

        public WorldPointerDisplaySlot(IWorldPointerDisplay<TContent> display) => _display = display;

        public override IWorldPointerDisplay Display => _display;

        public override Type ContentType => typeof(TContent);

        public override IWorldPointerIndicator Acquire() => _display.Acquire();

        public override void Release(IWorldPointerIndicator indicator) => _display.Release((IWorldPointerIndicator<TContent>) indicator);

        public override void SetContent(IWorldPointerIndicator indicator, object content) =>
            ((IWorldPointerIndicator<TContent>) indicator).SetContent((TContent) content);
    }
}
