// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_a_self_contained_slice : given.a_multi_module_application
{
    ArtifactRenderPlan _plan = null!;
    IReadOnlyList<string> _errors = [];
    IReadOnlyList<string> _warnings = [];

    void Because()
    {
        _plan = Plan(new(ArtifactRenderScopeKind.Slice, _placeOrder.Id));

        // Scoped plans target an already-scaffolded repository. Supply its registration surface,
        // not other slices' artifacts, just as the isolated generated policy specifications do.
        var registration = Files(_application).Single(file => file.RelativePath == "GeneratedPolicyRegistration.cs");
        var files = Files(_plan).Append(registration).ToArray();
        _errors = RenderedOutput.Errors(files);
        _warnings = RenderedOutput.Warnings(files);
    }

    [Fact] void should_plan_the_slice() => _plan.Success.ShouldBeTrue();
    [Fact] void should_compile_without_other_slices() => _errors.ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => _warnings.ShouldBeEmpty();
}
