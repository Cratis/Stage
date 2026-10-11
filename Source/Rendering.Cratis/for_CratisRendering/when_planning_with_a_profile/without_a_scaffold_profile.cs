// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class without_a_scaffold_profile : Specification
{
    CratisPlanOptions _options = new("Shop", "Shop.Backend", "Acme.Shop");
    PlannedArtifact[] _expected = [];
    CratisPlanResult _result = null!;

    void Establish() => _expected = [.. CratisRendering.CreateProfile(_options.ApplicationName, new(_options.ProjectName, _options.RootNamespace)).Inputs.Select(input =>
        CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) ? artifact! : throw new InvalidArtifactRenderContract("The default scaffold input could not be rendered."))];
    void Because() => _result = CratisRendering.PlanScaffold(_options);
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_default_artifact_count() => _result.Artifacts.Length.ShouldEqual(_expected.Length);
    [Fact] void should_preserve_every_default_scaffold_byte() => _result.Artifacts.All(artifact => _expected.Single(expected => expected.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
