// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_v32_whole_number_concept_capture_identities : given.a_translation_application
{
    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var capture = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate) with { Specifications = [] };
        _model = ExecutableSemanticModel.Create(LanguageVersion.V32, SemanticVersion.V32, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [capture] }] }]
        });
    }

    void Because()
    {
        var module = _model.Application.Modules.Single();
        var concept = _model.Application.Concepts.Single();
        _model = ExecutableSemanticModel.Create(LanguageVersion.V32, SemanticVersion.V32, _model.Application with
        {
            Concepts = [concept with { Primitive = SemanticPrimitiveType.WholeNumber }],
            Modules = [module]
        });
        Render();
    }

    [Fact]
    public void should_append_with_a_whole_number_concept_as_the_declared_identity()
    {
        var identity = _assembly.GetTypes().Single(type => type.Name == "OrderId");
        identity.BaseType!.GetGenericTypeDefinition().ShouldEqual(typeof(EventSourceId<>));
        new[] { typeof(int), typeof(long) }.ShouldContain(identity.BaseType.GetGenericArguments().Single());
        var reactor = Reactor();
        reactor.ShouldContain("EventForEventSourceId(@event.OrderId,");
        reactor.ShouldNotContain("@event.OrderId.ToString()");
        RenderedOutput.Errors(_sources).ShouldBeEmpty();
        RenderedOutput.Warnings(_sources).ShouldBeEmpty();
    }

    string Reactor() => _sources.Single(source => source.RelativePath.EndsWith("OrderIntake.cs", StringComparison.Ordinal)).Content;
}
#endif
