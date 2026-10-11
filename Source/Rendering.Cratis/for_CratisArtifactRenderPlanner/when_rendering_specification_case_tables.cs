// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_specification_case_tables : a_specification_case_table
{
    void Because() => _plan = when_rendering_a_pure_reducer.Plan(_loaded);

    [Fact] void should_admit_the_table() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_one_specification_class_per_case() => _plan.Artifacts.Where(artifact => Path.GetFileName(artifact.RelativePath).StartsWith("when_registering_", StringComparison.Ordinal)).Select(artifact => Path.GetFileName(artifact.RelativePath)).ShouldContainOnly("when_registering_small.cs", "when_registering_medium.cs", "when_registering_large.cs");
    [Fact] void should_substitute_the_small_case() => Text("small").ShouldContain("\"small\"");
    [Fact] void should_substitute_the_medium_case() => Text("medium").ShouldContain("\"medium\"");
    [Fact] void should_substitute_the_large_case() => Text("large").ShouldContain("\"large\"");
    [Fact] void should_leave_no_case_references() => string.Join('\n', _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ShouldNotContain("case.name");

    string Text(string name) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => Path.GetFileName(artifact.RelativePath) == $"when_registering_{name}.cs").Bytes.AsSpan());
}
#endif
