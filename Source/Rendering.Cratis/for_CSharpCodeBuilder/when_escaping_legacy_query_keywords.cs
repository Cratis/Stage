// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_escaping_legacy_query_keywords : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_escape_a_declared_by_event_parameter_and_its_reference(bool observable)
    {
        var location = SourceLocation.Start;
        var query = new QuerySyntax(
            "ForEvent",
            new("Summary", false, false, location),
            new("event", new("String", false, false, location), location),
            [],
            null,
            location) with { IsObservable = observable };
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "Summary", "string", "event", [query], new ApplicationSet([]), []);
        var source = builder.ToString();
        Assert.Contains(" @event)", source);
        Assert.Contains("(@event)", source);
        Assert.DoesNotContain(" event)", source);
    }

    [Fact]
    void should_escape_a_synthesized_state_view_lookup_parameter_and_its_reference()
    {
        var location = SourceLocation.Start;
        var property = new PropertySyntax("event", new("String", false, false, location), location);
        var from = new FromSyntax(
            [new("Registered", null, location)],
            null,
            null,
            [new SetMappingSyntax("event", new PathExpressionSyntax("event", location), location)],
            location);
        var projection = new ProjectionSyntax(
            "Summary",
            "Summary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("event", location), location),
            [from],
            location);
        var slice = new SliceSyntax(SliceType.StateView, "Summary", [new("Registered", [property], location)], [], [], [projection], [], [], [], [], [], location);
        var feature = new FeatureSyntax("Changes", [], [slice], location);
        var module = new ModuleSyntax("Shop", [], [feature], location);
        var application = new ApplicationSet([new ApplicationSyntax([], [], [], [module], location)]);
        var source = new StateViewSliceRenderer().Render(application.Slices.Single(), application, "Shop").Content;
        Assert.Contains("EventSourceId @event)", source);
        Assert.Contains("GetInstanceById<Summary>(@event)", source);
    }
}
