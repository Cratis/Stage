// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_typed_capture_keys_at_a_newer_minor : Specification
{
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan());
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var capture = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate) with { Specifications = [] };
        var application = _model.Application with { Modules = [module with { Features = [feature with { Slices = [capture] }] }] };
        _model = ExecutableSemanticModel.Create(LanguageVersion.V32, SemanticVersion.V32, application);
    }

    [Theory]
    [InlineData(SemanticPrimitiveType.Text, "@event.OrderId")]
    [InlineData(SemanticPrimitiveType.Uuid, "@event.OrderId")]
    [InlineData(SemanticPrimitiveType.WholeNumber, "new EventSourceId((@event.OrderId).ToString())")]
    public void should_preserve_complete_source_and_validation_at_a_newer_minor(SemanticPrimitiveType primitive, string expression)
    {
        var key = SemanticTypeReference.ForPrimitive(primitive);
        SetTypes(key, key);
        var audited = Context();
        var source = CaptureSource(audited);
        source.ShouldContain($"EventForEventSourceId({expression},");
        Check().ShouldBeNull();
        Versions(32, 1);
        var newer = Context();
        CaptureSource(newer).ShouldEqual(source);
        newer.IdentifierConcepts.Order().ShouldEqual(audited.IdentifierConcepts.Order());
        Check().ShouldBeNull();
    }

    [Fact]
    void should_preserve_capture_only_concept_identity_at_a_newer_minor()
    {
        var audited = Context();
        var id = _model.Application.Concepts.Single(concept => concept.Name == "OrderId").Id;
        audited.IdentifierConcepts.ShouldContain(id);
        var source = CaptureSource(audited);
        Versions(32, 1);
        var newer = Context();
        newer.IdentifierConcepts.ShouldContain(id);
        CaptureSource(newer).ShouldEqual(source);
        Check().ShouldBeNull();
    }

    [Fact]
    void should_preserve_unsafe_identity_refusal_at_a_newer_minor()
    {
        var key = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean);
        SetTypes(key, key);
        var audited = Check();
        audited!.ShouldContain("identity type");
        Versions(32, 1);
        Check().ShouldEqual(audited);
    }

    [Fact]
    void should_preserve_mismatched_append_identity_refusal_at_a_newer_minor()
    {
        SetTypes(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        var audited = Check();
        audited!.ShouldContain("will not coerce");
        Versions(32, 1);
        Check().ShouldEqual(audited);
    }

    [Theory]
    [InlineData(33)]
    [InlineData(34)]
    public void should_not_apply_the_v32_capture_branch_to_a_future_major(uint major)
    {
        SetTypes(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        Versions(major, 0);
        var context = Context();
        Check().ShouldBeNull();
        context.IdentifierConcepts.ShouldBeEmpty();
        CaptureSource(context).ShouldContain("new EventSourceId((@event.OrderId).ToString())");
    }

    SemanticApplicationContext Context()
    {
        var options = new CratisRenderingOptions("Orders", "Orders");
        var request = new ArtifactRenderRequest(_model, SemanticExecutionPlan.Compile(_model).Plan!, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        return new(request, options);
    }

    string? Check()
    {
        var context = Context();
        var located = context.AllSlices.Single();
        return SemanticCaptureIdentitySupport.Check(located.Slice.Captures.Single(), _model, context.Events);
    }

    static string CaptureSource(SemanticApplicationContext context)
    {
        var located = context.AllSlices.Single();
        return SemanticCaptureArtifactRenderer.Render(located, located.Slice.Captures.Single(), context).Content;
    }

    void Versions(uint major, uint minor)
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new LanguageVersion(major, minor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new SemanticVersion(major, minor));
    }

    void SetTypes(SemanticTypeReference key, SemanticTypeReference destination)
    {
        var slice = SemanticVersionFeatures.Slices(_model.Application).Single();
        var events = slice.Events.Select(contract => contract with { Properties = [.. contract.Properties.Select(property => property with { Type = key })] }).ToImmutableArray();
        var captures = slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { EventSourceType = destination })] }).ToImmutableArray();
        typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Events))!.SetValue(slice, events);
        typeof(SemanticSlice).GetProperty(nameof(SemanticSlice.Captures))!.SetValue(slice, captures);
    }
}
