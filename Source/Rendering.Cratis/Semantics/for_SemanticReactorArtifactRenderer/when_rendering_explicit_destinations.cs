// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer;

public class when_rendering_explicit_destinations : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    [Theory]
    [InlineData(false, "@event.Id")]
    [InlineData(true, "\"other\"")]
    public void should_keep_the_authored_destination(bool literal, string destination)
    {
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        var trigger = reaction.Triggers.Single();
        var id = _context.Events[trigger.Source].Properties.Single(property => property.Name == "id");
        var produced = trigger.Produces.Single() with
        {
            Destination = literal ? SemanticExpression.FromValue(SemanticValue.Text("other")) : SemanticExpression.Property(SemanticExpressionRootKind.Event, id.Id),
            DestinationType = id.Type
        };
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction with { Triggers = [trigger with { Produces = [produced] }] }, _context).Content;
        source.ShouldContain($"EventForEventSourceId({destination}, new");
    }

    [Fact]
    public void should_preserve_escaped_text_literals_without_overriding_occurrence()
    {
        SetContext(Compile(Source.Replace("scheduledAt DateTime", "message String", StringComparison.Ordinal)
            .Replace("scheduledAt = $context.occurred", "message = \"line\\nvalue\"", StringComparison.Ordinal)));
        Plan();
        _plan.Success.ShouldBeTrue();
        var reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        var source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        source.ShouldContain("Acknowledged(\"line\\u000Avalue\")");
        source.ShouldNotContain("Occurred = context.Occurred");
    }
}
