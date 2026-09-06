using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Pooling
{
    /// <summary>
    /// Parked instances, kept by the type they are. The framework pools four different things -
    /// bindings, commands, mediators and functions - and each had grown its own dictionary of
    /// stacks, so a fix or a guard on one of them was a fix on one of them.
    ///
    /// It holds instances and nothing else. What readies an instance for the next use differs by
    /// what it is - a binding is cleared, a command is cleaned, a function is disposed - so the
    /// caller does that before handing it over, and builds the instance itself when the pool has
    /// none. Neither belongs to a container of objects.
    /// </summary>
    internal sealed class TypePool<T> where T : class
    {
        private readonly Dictionary<Type, Stack<T>> _byType = new();

        /// <summary>
        /// What is parked, when this pool was asked to refuse a double return. Null otherwise: the
        /// check costs a hash of every returned instance, and only the mediators need it - a view
        /// unregistering twice would otherwise put one mediator in the pool for two views to find.
        /// </summary>
        private readonly HashSet<T> _parked;

        public TypePool(bool guardDoubleReturn = false)
        {
            if (guardDoubleReturn)
                _parked = new HashSet<T>();
        }

        public bool TryTake(Type type, out T item)
        {
            item = null;

            if (!_byType.TryGetValue(type, out Stack<T> parked) || parked.Count == 0)
                return false;

            item = parked.Pop();
            _parked?.Remove(item);

            return true;
        }

        /// <summary>
        /// Parks an instance. False when the pool already holds it and was asked to notice, which
        /// is the caller's cue that something returned the same instance twice.
        /// </summary>
        public bool Return(Type type, T item)
        {
            if (item == null)
                return false;

            if (_parked != null && !_parked.Add(item))
                return false;

            if (!_byType.TryGetValue(type, out Stack<T> parked))
            {
                parked = new Stack<T>();
                _byType[type] = parked;
            }

            parked.Push(item);
            return true;
        }

        /// <summary>
        /// Lets go of everything parked. What one run built is of no use to the next, and a burst
        /// would otherwise be held for as long as the run lasts.
        /// </summary>
        public void Clear()
        {
            _byType.Clear();
            _parked?.Clear();
        }
    }
}
