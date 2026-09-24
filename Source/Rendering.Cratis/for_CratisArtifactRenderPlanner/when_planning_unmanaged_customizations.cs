// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_unmanaged_customizations : a_register_project_render_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;
    readonly byte[] _customStyles = "/* product CSS */"u8.ToArray();
    readonly byte[] _customCode = "// product services"u8.ToArray();
    Dictionary<string, byte[]> _files = null!;

    void Because()
    {
        _first = _planner.Plan(_request);
        _files = new(StringComparer.Ordinal)
        {
            ["Customizations/styles.css"] = _customStyles,
            ["Customizations/Program.cs"] = _customCode
        };
        foreach (var artifact in _first.Artifacts)
        {
            _files[artifact.RelativePath] = [.. artifact.Bytes];
        }

        _second = _planner.Plan(_request);
        foreach (var artifact in _second.Artifacts)
        {
            _files[artifact.RelativePath] = [.. artifact.Bytes];
        }
    }

    [Fact] void should_admit_both_plans() => (_first.Success && _second.Success).ShouldBeTrue();
    [Fact] void should_plan_managed_artifacts() => _first.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_keep_the_managed_artifact_count() => _second.Artifacts.Length.ShouldEqual(_first.Artifacts.Length);
    [Fact] void should_not_plan_unmanaged_paths() => _second.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("Customizations/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_repeat_the_managed_bytes() => _second.Artifacts.Zip(_first.Artifacts).All(pair => pair.First.RelativePath == pair.Second.RelativePath && pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_leave_stylesheet_bytes_outside_the_plan_unchanged() => _files["Customizations/styles.css"].SequenceEqual(_customStyles).ShouldBeTrue();
    [Fact] void should_leave_service_bytes_outside_the_plan_unchanged() => _files["Customizations/Program.cs"].SequenceEqual(_customCode).ShouldBeTrue();
}
