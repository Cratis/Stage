// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_supplying_the_scaffold_to_the_application_planner : a_register_project_render_request
{
    const string InputNamePrefix = "cratis-scaffold:text:";
    ImmutableArray<ArtifactRenderInput> _inputs;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);
        _inputs = profile.Inputs;
        _request = _request with { Profile = profile };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_admit_the_existing_register_project_semantics() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_existing_register_project_slice() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs").ShouldBeTrue();
    [Fact] void should_emit_every_scaffold_input_byte_for_byte() => _inputs.All(InputIsUnchanged).ShouldBeTrue();
    [Fact] void should_preserve_every_scaffold_hash() => _inputs.All(InputHashIsUnchanged).ShouldBeTrue();

    bool InputIsUnchanged(ArtifactRenderInput input)
    {
        var artifact = _plan.Artifacts.Single(candidate => candidate.RelativePath == input.Name[InputNamePrefix.Length..]);
        return artifact.Kind == PlannedArtifactKind.Text && artifact.Bytes.SequenceEqual(input.Bytes);
    }

    bool InputHashIsUnchanged(ArtifactRenderInput input)
    {
        var artifact = _plan.Artifacts.Single(candidate => candidate.RelativePath == input.Name[InputNamePrefix.Length..]);
        return artifact.Sha256 == input.Sha256;
    }
}
