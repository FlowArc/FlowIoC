#if UNITY_EDITOR

using System.Text;
using FlowIoC.ScreenModule.Data;

namespace FlowIoC.Editor.CodeGenerator.Screens
{
    /// <summary>
    /// Renders a screen module's context. There is no template file on disk for this one because
    /// the Screen block changes with every input, and a string builder is easier to keep honest
    /// than a placeholder scan. The SignalBindings and CommandBindings overrides are emitted even
    /// though they are empty: BindSignalsInContext and the command generator insert after the
    /// "base.X();" lines, so the anchors have to be there.
    /// </summary>
    internal class ScreenContextTemplate
    {
        internal string Render(string namespaceName, string contextName, string viewName, string mediatorName,
            string viewNamespace, ScreenModuleSettings settings)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("using FlowIoC.ScreenModule.Data;");
            builder.AppendLine("using FlowIoC.ScreenModule.Enums;");
            builder.AppendLine("using FlowIoC.ScreenModule.RootsContexts;");
            builder.AppendLine($"using {viewNamespace};");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName}");
            builder.AppendLine("{");
            builder.AppendLine($"    public class {contextName} : ScreenSubContext<{viewName}, {mediatorName}>");
            builder.AppendLine("    {");
            builder.Append(Indent(RenderScreenBlock(settings), "        "));
            builder.AppendLine();
            builder.AppendLine("        public override void SignalBindings()");
            builder.AppendLine("        {");
            builder.AppendLine("            base.SignalBindings();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public override void CommandBindings()");
            builder.AppendLine("        {");
            builder.AppendLine("            base.CommandBindings();");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// The Screen property alone, unindented. The migrator logs this for a context that already
        /// exists, so the owner can paste it in.
        /// </summary>
        internal string RenderScreenBlock(ScreenModuleSettings settings)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("protected override ScreenCVO Screen => new()");
            builder.AppendLine("{");
            builder.AppendLine($"    ManagerId = {settings.ManagerId},");
            builder.AppendLine($"    Layer = {settings.Layer},");
            builder.AppendLine($"    Tag = ScreenTag.{settings.Tag},");
            builder.AppendLine($"    Load = {RenderLoad(settings)},");
            builder.AppendLine($"    HasShowAnimation = {Bool(settings.HasShowAnimation)},");
            builder.AppendLine($"    HasHideAnimation = {Bool(settings.HasHideAnimation)},");
            builder.AppendLine("};");

            return builder.ToString();
        }

        private static string RenderLoad(ScreenModuleSettings settings)
        {
            return settings.LoadType == ScreenLoadType.Resource
                ? $"ScreenLoadCVO.Resource(\"{Escape(settings.ResourcePath)}\")"
                : $"ScreenLoadCVO.Addressable(\"{Escape(settings.AddressableKey)}\")";
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string Bool(bool value) => value ? "true" : "false";

        private static string Indent(string block, string indent)
        {
            string[] lines = block.TrimEnd('\r', '\n').Split('\n');
            StringBuilder builder = new StringBuilder();

            foreach (string line in lines)
                builder.AppendLine(line.Length == 0 ? line : indent + line.TrimEnd('\r'));

            return builder.ToString();
        }
    }
}

#endif
