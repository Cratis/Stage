// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_the_scaffold_only : Specification
{
    CratisPlanResult _result = null!;
    void Because() => _result = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop"));
    [Fact] void should_plan_without_sources() => _result.Success.ShouldBeTrue();
    [Fact] void should_place_the_project_at_the_application_root() => _result.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("Shop.Backend.csproj");
    [Fact] void should_place_program_at_the_application_root() => _result.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("Program.cs");
    [Fact] void should_have_no_semantic_revision() => _result.Revision.ShouldBeNull();
    [Fact] void should_have_no_model_plan() => _result.Plan.ShouldBeNull();
    [Fact] void should_have_no_policy_files_to_check_before_publication() => _result.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/", StringComparison.Ordinal) || artifact.RelativePath.StartsWith("TypedContexts/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_have_no_semantic_sources() => _result.Artifacts.All(artifact => artifact.Sources.IsEmpty).ShouldBeTrue();
    [Fact] void should_have_an_output_digest() => _result.Digest.Length.ShouldEqual(64);
}
