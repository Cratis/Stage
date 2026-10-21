// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_seeded_composite_removal_without_an_event_source : Specification
{
    SemanticSpecification _specification = null!;
    ArtifactRenderRequest _request = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var compilation = SemanticExecutionPlan.Compile(model);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Issues));
        _specification = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Specifications).Single(specification => specification.Name == "RemoveOnlyMatchingParts");
        _request = new(
            model,
            compilation.Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("CompositeRemoval", "CompositeRemoval")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_cover_seeded_absence() => _specification.GivenReadModels.ShouldNotBeEmpty();
    [Fact] void should_cover_an_absence_expectation() => _specification.ThenAbsentReadModels.ShouldNotBeEmpty();
    [Fact] void should_cover_an_unsourced_append() => Assert.True(_specification.WhenAppended is { EventSource: null });
    [Fact] void should_not_refuse_the_unobserved_event_source() => Assert.DoesNotContain(_plan.Diagnostics, diagnostic => diagnostic.Artifact == _specification.Id && diagnostic.Code == "STAGE-ESM-051");
    [Fact] void should_keep_the_independent_seeded_absence_refusal() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Artifact == _specification.Id && diagnostic.Code == "STAGE-ESM-011");
    [Fact] void should_not_admit_composite_absence() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Artifact == _specification.Id && diagnostic.Code == "STAGE-ESM-027");
    [Fact] void should_not_publish_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
