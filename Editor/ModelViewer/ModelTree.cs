#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Contexts;
using FlowIoC.BaseModule.Injectable;
using FlowIoC.BaseModule.Injectable.CrossContext;
using FlowIoC.BaseModule.Root;
using FlowIoC.BaseModule.Signals;
using FlowIoC.Editor.Inspector;
using Object = UnityEngine.Object;

namespace FlowIoC.Editor.ModelViewer
{
    /// <summary>
    /// What the Model Viewer lists above the members: every Root in Initialize Order, and under
    /// each the objects its contexts bound - in the context's own binder, and across when the
    /// context is the one that bound them. The framework's plumbing is left out by the attribute
    /// the binders and providers carry, the GameObject entries and the providers by being
    /// UnityEngine.Objects, and a signal holder because it is the module's surface and not its
    /// state.
    ///
    /// Pure: the window hands in the Roots it found, and a test hands in contexts it built.
    /// </summary>
    internal class ModelTree
    {
        private readonly FlowRoleResolver _roles = new FlowRoleResolver();

        /// <summary>
        /// The rows for these Roots, in Initialize Order and then by name. A Root without a
        /// context - not started - is left out; its module has bound nothing yet.
        /// </summary>
        public List<ModelRootEVO> Build(IReadOnlyList<RootBase> roots)
        {
            var rows = new List<ModelRootEVO>();

            IOrderedEnumerable<RootBase> ordered = roots
                .Where(root => root != null && root.Context != null)
                .OrderBy(root => root.initializeOrder)
                .ThenBy(root => root.name, StringComparer.Ordinal);

            foreach (RootBase root in ordered)
            {
                var row = new ModelRootEVO {Root = root, Role = RoleOf(root)};
                List<IContext> contexts = root.Context.AllContexts ?? new List<IContext> {root.Context};

                for (int ii = 0; ii < contexts.Count; ii++)
                {
                    List<ModelObjectEVO> objects = Objects(contexts[ii]);

                    if (ii == 0)
                        row.Objects = objects;
                    else if (objects.Count > 0)
                        row.SubContexts.Add(new ModelContextEVO {Context = contexts[ii], Objects = objects});
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// The objects one context bound, filtered and in kind order: its own binder's, then those
        /// in the shared binder that name it as the context that bound them.
        /// </summary>
        public List<ModelObjectEVO> Objects(IContext context)
        {
            var objects = new List<ModelObjectEVO>();

            if (context.InjectionBinder != null)
            {
                foreach (InjectionBinding binding in context.InjectionBinder.GetAllInjectionBindings())
                    TryAdd(objects, binding);
            }

            if (context.InjectionBinderCrossContext != null)
            {
                foreach (InjectionBinding binding in context.InjectionBinderCrossContext.GetAllInjectionBindings())
                {
                    if (binding.BoundContext == context)
                        TryAdd(objects, binding);
                }
            }

            return Sorted(objects);
        }

        /// <summary>
        /// What was bound across before any context existed - the RootsManager's own filings,
        /// today the shared data model - so what is filed as shared is readable in the same window.
        /// </summary>
        public List<ModelObjectEVO> Shared(InjectionBinderCrossContext binder)
        {
            var objects = new List<ModelObjectEVO>();

            foreach (InjectionBinding binding in binder.GetAllInjectionBindings())
            {
                if (binding.BoundContext == null)
                    TryAdd(objects, binding);
            }

            return Sorted(objects);
        }

        /// <summary>
        /// Whether a bound value is the module's state rather than the framework's plumbing or the
        /// module's surface.
        /// </summary>
        public bool IsListed(object value)
        {
            if (value == null || value is Object || value is ISignalHolder) return false;

            return !value.GetType().IsDefined(typeof(HideInModelViewerAttribute), true);
        }

        /// <summary>The kind a type's name says it is, the longer suffixes tested before the ones they end in.</summary>
        public ModelKind KindOf(Type type)
        {
            string name = type.Name;

            if (name.EndsWith("SubService", StringComparison.Ordinal)) return ModelKind.SubService;
            if (name.EndsWith("SubSystem", StringComparison.Ordinal)) return ModelKind.SubSystem;
            if (name.EndsWith("Model", StringComparison.Ordinal)) return ModelKind.Model;
            if (name.EndsWith("Service", StringComparison.Ordinal)) return ModelKind.Service;
            if (name.EndsWith("System", StringComparison.Ordinal)) return ModelKind.System;

            return ModelKind.Other;
        }

        /// <summary>Adds the binding's value unless it is filtered out or the same instance is already listed under an earlier key.</summary>
        private void TryAdd(List<ModelObjectEVO> objects, InjectionBinding binding)
        {
            object value = binding.Value;

            if (!IsListed(value)) return;

            foreach (ModelObjectEVO listed in objects)
            {
                if (ReferenceEquals(listed.Value, value)) return;
            }

            objects.Add(new ModelObjectEVO
            {
                Value = value,
                Key = binding.Key as Type,
                Name = binding.Name ?? "",
                Kind = KindOf(value.GetType())
            });
        }

        private List<ModelObjectEVO> Sorted(List<ModelObjectEVO> objects)
        {
            return objects
                .OrderBy(entry => entry.Kind)
                .ThenBy(entry => entry.Value.GetType().Name, StringComparer.Ordinal)
                .ToList();
        }

        private FlowRole RoleOf(RootBase root) => _roles.TryResolve(root.GetType(), out FlowRole role) ? role : FlowRole.Root;
    }
}

#endif
