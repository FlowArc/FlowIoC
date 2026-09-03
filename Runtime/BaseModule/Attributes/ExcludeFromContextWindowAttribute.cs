using System;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// Keeps a context out of the Root inspector's Add Sub Context list. It is the last word on
    /// the matter: a context carrying it stays hidden even when it also carries
    /// <see cref="AllowAsSubContextAttribute"/>.
    ///
    /// Not inherited, so hiding one context never hides whatever derives from it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ExcludeFromContextWindowAttribute : Attribute
    {
    }
}