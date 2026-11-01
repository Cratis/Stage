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
    [Fact] void should_preserve_the_inventory_absent_digest() => ArtifactRenderPlan.ComputeDigest(_existing.Target, _existing.TargetVersion, _existing.Renderer, _existing.RendererVersion, _existing.ApplicationName, [.. _result.Artifacts.Where(artifact => !IsReport(artifact))], _result.Plan!.Diagnostics).ShouldEqual(_existing.Digest);
    [Fact] void should_preserve_all_payload_bytes() => _result.Artifacts.Where(artifact => !IsReport(artifact)).Zip(_existing.Artifacts).All(pair => pair.First.RelativePath == pair.Second.RelativePath && pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_add_exactly_two_managed_reports() => _result.Artifacts.Length.ShouldEqual(_existing.Artifacts.Length + 2);
    [Fact] void should_expose_both_managed_report_paths() => _result.Artifacts.Where(IsReport).Select(artifact => artifact.RelativePath).ShouldContainOnly([".stage/completeness.json", "UNSUPPORTED.md"]);

    static bool IsReport(PlannedArtifact artifact) => string.Equals(artifact.RelativePath, ".stage/completeness.json", StringComparison.Ordinal) || string.Equals(artifact.RelativePath, "UNSUPPORTED.md", StringComparison.Ordinal);
}
