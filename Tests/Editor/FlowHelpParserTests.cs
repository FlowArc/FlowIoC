using System.Collections.Generic;
using FlowIoC.Editor.Inspector;
using NUnit.Framework;

namespace FlowIoC.Tests
{
    public class FlowHelpParserTests
    {
        [Test]
        public void Parse_reads_the_summary_above_a_field()
        {
            const string source = @"
public class RootBase
{
    /// <summary>
    /// Roots bind in this order.
    /// </summary>
    public int initializeOrder;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.AreEqual("Roots bind in this order.", help["initializeOrder"]);
        }

        [Test]
        public void Parse_reads_past_an_attribute_on_the_same_line()
        {
            const string source = @"
public class RootBase
{
    /// <summary>Runs Setup on its own.</summary>
    [HideInInspector] public bool AutoSetup = true;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.AreEqual("Runs Setup on its own.", help["AutoSetup"]);
        }

        [Test]
        public void Parse_joins_a_summary_that_spans_lines_into_one_paragraph()
        {
            const string source = @"
public class RootBase
{
    /// <summary>
    /// Setup runs a frame after every Root has finished binding.
    /// That barrier is what makes crossing modules safe.
    /// </summary>
    public bool AutoSetup;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.AreEqual(
                "Setup runs a frame after every Root has finished binding. That barrier is what makes crossing modules safe.",
                help["AutoSetup"]);
        }

        [Test]
        public void Parse_flattens_a_see_reference_to_its_last_segment()
        {
            const string source = @"
public class RootBase
{
    /// <summary>Mirrors <see cref=""RootBase.AutoSetup""/> for Launch.</summary>
    public bool AutoLaunch;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.AreEqual("Mirrors AutoSetup for Launch.", help["AutoLaunch"]);
        }

        [Test]
        public void Parse_keeps_the_type_summary_under_its_own_key()
        {
            const string source = @"
/// <summary>The module's one presence in the scene.</summary>
public class RootBase
{
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.AreEqual("The module's one presence in the scene.", help[FlowHelpParser.TypeKey]);
        }

        [Test]
        public void Parse_skips_a_field_that_has_no_comment()
        {
            const string source = @"
public class RootBase
{
    public int initializeOrder;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.IsFalse(help.ContainsKey("initializeOrder"));
        }

        [Test]
        public void Parse_drops_a_comment_that_a_blank_line_separates_from_the_field()
        {
            const string source = @"
public class RootBase
{
    /// <summary>Not attached to anything.</summary>

    public int initializeOrder;
}";

            Dictionary<string, string> help = new FlowHelpParser().Parse(source);

            Assert.IsFalse(help.ContainsKey("initializeOrder"));
        }
    }
}
