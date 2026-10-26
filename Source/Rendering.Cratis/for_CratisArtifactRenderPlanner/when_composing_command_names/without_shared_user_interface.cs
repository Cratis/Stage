// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class without_shared_user_interface : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        DuplicateCommandName();
        _request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, _registerProject.Id) };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_not_report_ambiguity() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").ShouldBeFalse();
    [Fact] void should_preserve_the_backend_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_backend_artifacts() => _plan.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_not_emit_shared_user_interface() => _plan.Artifacts.Any(artifact => UserInterfacePaths.Contains(artifact.RelativePath)).ShouldBeFalse();
}
