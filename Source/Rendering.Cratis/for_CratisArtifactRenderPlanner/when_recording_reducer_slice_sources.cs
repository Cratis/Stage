// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_recording_reducer_slice_sources : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;
    SemanticSlice _declaring = null!;
    SemanticSlice _input = null!;
    PlannedArtifact _reducer = null!;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        _loaded = await when_rendering_a_pure_reducer.Load(source);
        var slices = _loaded.Model.Application.Modules.Single().Features.Single().Slices;
        _declaring = slices.Single(slice => slice.Reducers.Length > 0);
        _input = slices.Single(slice => slice.Events.Length > 0);
    }

    void Because()
    {
        _first = when_rendering_a_pure_reducer.Plan(_loaded);
        _second = when_rendering_a_pure_reducer.Plan(_loaded);
        _reducer = _first.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal));
    }

    [Fact] void should_admit_the_reducer() => _first.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_read_model_and_input_events_alongside_the_declaring_slice() => _reducer.Sources.ShouldEqual(new[] { _declaring.ReadModels.Single().Id, _input.Events.Single().Id, _declaring.Id }.OrderBy(id => id.ToString(), StringComparer.Ordinal));
    [Fact] void should_not_name_the_input_events_slice_as_an_owner() => _reducer.Sources.ShouldNotContain(_input.Id);
    [Fact] void should_preserve_the_previous_artifact_hash() => _reducer.Sha256.ShouldEqual("c7418e2f6622e9bc6c04928cda1004d2cee67ef0e381bc4fd48d2ce5c3cd9e55");
    [Fact] void should_repeat_the_same_sources() => _second.Artifacts.Single(artifact => artifact.RelativePath == _reducer.RelativePath).Sources.AsEnumerable().ShouldEqual(_reducer.Sources.AsEnumerable());
    [Fact] void should_repeat_the_same_plan_digest() => _second.Digest.ShouldEqual(_first.Digest);
}
