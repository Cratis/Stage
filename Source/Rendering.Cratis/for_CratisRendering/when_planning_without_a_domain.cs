// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_without_a_domain : given.a_source_plan
{
    ArtifactRenderPlan _existing = null!;
    void Establish() => _existing = CratisRendering.Plan(_loaded.Model, _loaded.Plan, new(ArtifactRenderScopeKind.Feature, _loaded.Model.Application.Modules.Single(module => module.Name == "Projects").Features.Single().Id), _options);
    void Because() => _result = CratisRendering.PlanFrom(_loaded, _featureSelection, _planOptions);
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_preserve_the_existing_digest() => _result.Digest.ShouldEqual(_existing.Digest);
    [Fact] void should_preserve_all_artifact_bytes() => _result.Artifacts.Zip(_existing.Artifacts).All(pair => pair.First.RelativePath == pair.Second.RelativePath && pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_preserve_the_artifact_count() => _result.Artifacts.Length.ShouldEqual(_existing.Artifacts.Length);
}
