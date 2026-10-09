// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_placing_under_a_domain : given.a_source_plan
{
    IReadOnlyList<string> _errors = null!;
    CratisPlanResult _scaffold = null!;
    void Establish() => _scaffold = CratisRendering.PlanScaffold(_planOptions);
    void Because()
    {
        _result = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Module("Projects"), PlanSelectionEntry.Module("Tasks")]), _planOptions with { Domain = "sales/retail" });
        var files = _result.Artifacts.Concat(_scaffold.Artifacts)
            .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        _errors = RenderedOutput.Errors(files);
    }
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_prefix_slice_paths() => Paths(_result).ShouldContain("Sales/Retail/Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_prefix_common_paths() => Paths(_result).ShouldContain("Sales/Retail/Common/ProjectId.cs");
    [Fact] void should_compile_domain_output_with_root_scaffold_helpers() => _errors.ShouldBeEmpty();
}
