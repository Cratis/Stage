// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_combined_scaffold_and_localized_slice : given.a_profile_source_plan
{
    CratisPlanResult _scaffold = null!;
    PlannedArtifact[] _combined = [];
    IReadOnlyList<string> _errors = [];

    protected override string ModelSource => base.ModelSource.Replace("        produces ProjectRegistered", "        validate\n          name not empty message $strings.project.name\n        produces ProjectRegistered", StringComparison.Ordinal);

    void Establish()
    {
        _selection = new([PlanSelectionEntry.Slice("Projects", "Registration", "RegisterProject")]);
        PlanFull();
    }

    void Because()
    {
        _scaffold = CratisRendering.PlanScaffold(_options);
        _result = CratisRendering.PlanFrom(_loaded, _selection, _options);
        _combined = [.. _scaffold.Artifacts, .. _result.Artifacts];

        // The existing in-memory backend harness excludes the SDK-owned top-level host entry point.
        _errors = RenderedOutput.Errors(_combined.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))));
    }

    [Fact] void should_plan_the_scaffold_successfully() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_plan_the_slice_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_a_successful_full_baseline() => _full.Success.ShouldBeTrue();
    [Fact] void should_have_no_duplicate_paths() => _combined.Select(artifact => artifact.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldEqual(_combined.Length);
    [Fact] void should_match_every_full_render_artifact_byte() => _combined.All(artifact => _full.Artifacts.Single(full => full.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
    [Fact] void should_supply_the_strings_runtime() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("GeneratedStrings.cs");
    [Fact] void should_configure_program_localization() => Encoding.UTF8.GetString(_scaffold.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs").Bytes.AsSpan()).ShouldContain("app.UseRequestLocalization");
    [Fact] void should_compile_the_combined_backend() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
}
