// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer;

/// <summary>
/// Screenplay 3 lets a reaction be set off by the clock. A Chronicle reactor is set off by an event and has
/// no shape for a schedule, so there is nothing to render - and a construct that renders to nothing and says
/// nothing is indistinguishable from one that was never declared.
/// </summary>
public class when_rendering_a_reaction_the_clock_sets_off : Specification
{
    ReactionSliceRenderer _renderer = null!;
    RenderedFile _rendered = null!;

    void Establish() => _renderer = new ReactionSliceRenderer();

    void Because()
    {
        var reaction = new ReactionSyntax(
            "OverdueChaser",
            [
                new ReactionTriggerSyntax(new IntervalTriggerSourceSyntax(15, IntervalUnit.Minutes, SourceLocation.Start), [], null, null, SourceLocation.Start),
                new ReactionTriggerSyntax(new ScheduleTriggerSourceSyntax(new TimeOnly(8, 0), null, null, SourceLocation.Start), [], null, null, SourceLocation.Start)
            ],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.Automation, "ChaseOverdue", [], [], [], [], [], [reaction], [], [], [], SourceLocation.Start);

        _rendered = _renderer.Render(new LocatedSlice(slice, ["Billing", "Invoices"]), new ApplicationSet([]), "Acme");
    }

    [Fact] void should_report_both_triggers() => _rendered.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_name_the_interval() => _rendered.Diagnostics.ShouldContain("Reaction 'OverdueChaser' is set off 'every 15 minutes', which has no Chronicle equivalent - it is not rendered");
    [Fact] void should_name_the_schedule() => _rendered.Diagnostics.ShouldContain("Reaction 'OverdueChaser' is set off 'at 08:00', which has no Chronicle equivalent - it is not rendered");
    [Fact] void should_say_so_in_the_generated_file() => _rendered.Content.ShouldContain("a Chronicle reactor is set off by an event, so this is not rendered");
}
