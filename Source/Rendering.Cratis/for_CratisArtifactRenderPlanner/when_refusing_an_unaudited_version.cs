// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_unaudited_version : a_register_project_render_request
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        // Simulate dependency versions Stage has not audited, including the hole before v23.
        _plans = [.. new uint[] { 22, 25 }.Select(version =>
        {
            typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new LanguageVersion(version, 0));
            typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new SemanticVersion(version, 0));
            return _planner.Plan(_request);
        })];
    }

    [Fact] void should_refuse_by_version() => _plans.ShouldEachConformTo(plan => plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().SequenceEqual(["STAGE-ESM-016"]));
    [Fact] void should_emit_no_artifacts() => _plans.ShouldEachConformTo(plan => plan.Artifacts.IsEmpty);
}
