// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_unique_names : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_the_original_bare_binding() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "src/bindings.ts")).ShouldContain("\"RegisterProject\": __sceneCommand");
    [Fact] void should_keep_the_additional_bare_binding() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "src/bindings.ts")).ShouldContain("\"OtherRegisterProject\": __sceneCommand");
    [Fact] void should_not_report_ambiguity() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").ShouldBeFalse();
    [Fact] void should_admit_the_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_all_shared_user_interface() => UserInterfacePaths.All(path => _plan.Artifacts.Any(artifact => artifact.RelativePath == path)).ShouldBeTrue();
}
