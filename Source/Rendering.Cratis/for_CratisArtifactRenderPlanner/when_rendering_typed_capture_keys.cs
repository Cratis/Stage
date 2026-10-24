// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_typed_capture_keys : given.a_translation_application
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

    [Fact]
    void should_render_a_capture_only_concept_as_an_event_source_identity()
    {
        Render();
        var identity = _assembly.GetTypes().Single(type => type.Name == "OrderId");
        identity.BaseType!.GetGenericTypeDefinition().ShouldEqual(typeof(EventSourceId<>));
        var reactor = _sources.Single(source => source.RelativePath.EndsWith("OrderIntake.cs", StringComparison.Ordinal)).Content;
        reactor.ShouldContain("EventForEventSourceId(@event.OrderId,");
        reactor.ShouldNotContain("@event.OrderId.ToString()");
        RenderedOutput.Errors(_sources).ShouldBeEmpty();
        RenderedOutput.Warnings(_sources).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Text, "@event.OrderId")]
    [InlineData(SemanticPrimitiveType.Uuid, "@event.OrderId")]
    [InlineData(SemanticPrimitiveType.WholeNumber, "new global::Cratis.Chronicle.Events.EventSourceId((@event.OrderId).ToString())")]
    public void should_compile_supported_primitive_identities(SemanticPrimitiveType primitive, string expression)
    {
        var key = SemanticTypeReference.ForPrimitive(primitive);
        SetTypes(key, key);
        Render();
        _sources.Single(source => source.RelativePath.EndsWith("OrderIntake.cs", StringComparison.Ordinal)).Content
            .ShouldContain($"EventForEventSourceId({expression},");
        RenderedOutput.Errors(_sources).ShouldBeEmpty();
        RenderedOutput.Warnings(_sources).ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_a_text_fallback_instead_of_losing_the_declared_identity()
    {
        var slice = SemanticVersionFeatures.Slices(_model.Application).Single();
        var capture = slice.Captures.Single();
        capture = capture with { Appends = [capture.Appends.Single() with { EventSourceType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) }] };
        SemanticCaptureIdentitySupport.Check(capture, _model, slice.Events.ToDictionary(contract => contract.Id))!.ShouldContain("will not coerce");
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Boolean)]
    [InlineData(SemanticPrimitiveType.DecimalNumber)]
    public void should_refuse_identity_types_without_an_audited_conversion(SemanticPrimitiveType primitive)
    {
        var key = SemanticTypeReference.ForPrimitive(primitive);
        SetTypes(key, key);
        var plan = Plan(_model);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-052");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void should_refuse_a_key_that_is_not_one_required_scalar(bool optional, bool collection)
    {
        var key = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) with { IsOptional = optional, IsCollection = collection };
        var slice = SemanticVersionFeatures.Slices(_model.Application).Single();
        var capture = slice.Captures.Single();
        var events = slice.Events.Select(contract => contract with
        {
            Properties = [.. contract.Properties.Select(property => property with { Type = key })]
        }).ToDictionary(contract => contract.Id);
        SemanticCaptureIdentitySupport.Check(capture, _model, events)!.ShouldContain("one required scalar");
    }

    void SetTypes(SemanticTypeReference key, SemanticTypeReference destination)
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with
        {
            Events = [.. slice.Events.Select(contract => contract with { Properties = [.. contract.Properties.Select(property => property with { Type = key })] })],
            Captures = [.. slice.Captures.Select(capture => capture with
            {
                Appends = [.. capture.Appends.Select(append => append with { EventSourceType = destination })]
            })]
        });
        var application = _model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        var text = destination == SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
        _model = ExecutableSemanticModel.Create(text ? LanguageVersion.V9 : LanguageVersion.V32, text ? SemanticVersion.V9 : SemanticVersion.V32, application);
    }
}
#endif
