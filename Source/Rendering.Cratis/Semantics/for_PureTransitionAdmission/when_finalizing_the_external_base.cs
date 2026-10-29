// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_PureTransitionAdmission;

public class when_finalizing_the_external_base : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;
    string _reducer = null!;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        _loaded = await when_rendering_a_pure_reducer.Load(source);
    }

    void Because()
    {
        _plan = when_rendering_a_pure_reducer.Plan(_loaded);
        _reducer = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_admit_the_verified_transition() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_the_short_base() => _reducer.ShouldContain("public class Fold : IReducerFor<Total>");
    [Fact] void should_not_leave_the_base_qualified() => _reducer.ShouldNotContain("global::Cratis.Chronicle.Reducers.IReducerFor");
}
