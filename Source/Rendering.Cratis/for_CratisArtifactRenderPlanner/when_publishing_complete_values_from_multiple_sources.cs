// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_publishing_complete_values_from_multiple_sources : given.a_multi_source_publication
{
    void Establish() => _model = Transform(_model, slice => slice with
    {
        Projections = [.. slice.Projections.Select(projection => projection with
        {
            Scope = projection.Scope! with
            {
                From = [.. projection.Scope!.From.Select(source => Complete(source, slice.Events.Single()))]
            }
        })]
    });

    void Because() => Render();

    [Fact] void should_admit_each_complete_source() => _plan.Success.ShouldBeTrue();
    [Fact] void should_compile_the_generated_handlers() => RenderedOutput.Errors(_sources).ShouldBeEmpty();

    static SemanticProjectionFrom Complete(SemanticProjectionFrom source, SemanticEventContract target) => source with
    {
        Mappings = [.. source.Mappings, .. target.Properties
            .Where(property => !source.Mappings.Any(mapping => mapping.Target[0] == property.Id))
            .Select(property => new SemanticProjectionMapping([property.Id], SemanticProjectionOperation.Set, Literal(property)))]
    };

    static SemanticProjectionLiteral Literal(SemanticProperty property) => new(
        property.Type.Primitive == SemanticPrimitiveType.WholeNumber ? SemanticValue.Number(0) : SemanticValue.Text("unknown"));
}
#endif
