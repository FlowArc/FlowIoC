using System;

namespace FlowIoC.BaseModule.Attributes
{
    /// <summary>
    /// Puts a context back in the Root inspector's Add Sub Context list after its own Root has
    /// taken it out. A context that some Root declares as its Root&lt;T&gt; is built by that Root
    /// already, so listing it again would build a second instance and run the same bindings
    /// twice - the list hides it. A module meant to be hosted on another module's Root instead
    /// says so here.
    ///
    /// Not inherited: the exception belongs to the context that asked for it and not to whatever
    /// derives from it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AllowAsSubContextAttribute : Attribute
    {
    }
}
