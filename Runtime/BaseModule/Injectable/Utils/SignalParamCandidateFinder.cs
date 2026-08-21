using System;
using System.Collections.Generic;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    /// <summary>
    /// Works out which slots of a dispatched signal payload can supply a value for a
    /// given property type, in payload order.
    /// </summary>
    internal sealed class SignalParamCandidateFinder
    {
        private readonly List<int> _exact = new List<int>();
        private readonly List<int> _assignable = new List<int>();

        /// <summary>
        /// Exact type matches win outright; the assignable pass is consulted only when
        /// nothing matched exactly. A null counts in both passes for any type that can
        /// hold null, so a dispatched null does not shift the slots that follow it.
        /// </summary>
        public List<int> Find(Type type, object[] values)
        {
            var candidates = new List<int>();
            if (type == null || values == null)
                return candidates;

            _exact.Clear();
            _assignable.Clear();

            // A boxed int arrives as Int32, never as Nullable<Int32>, so an int?
            // property has to be matched against its underlying type.
            Type effective = Nullable.GetUnderlyingType(type) ?? type;
            bool acceptsNull = CanHoldNull(type);

            for (int i = 0; i < values.Length; i++)
            {
                object value = values[i];

                if (value == null)
                {
                    if (acceptsNull)
                    {
                        _exact.Add(i);
                        _assignable.Add(i);
                    }
                    continue;
                }

                if (value.GetType() == effective)
                    _exact.Add(i);

                if (effective.IsInstanceOfType(value))
                    _assignable.Add(i);
            }

            candidates.AddRange(_exact.Count > 0 ? _exact : _assignable);
            return candidates;
        }

        public bool CanHoldNull(Type type)
            => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}
