// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_failed_compilation_also_reports_information : given.a_source_plan
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "broken.play"), "not valid screenplay !!!");
    async Task Because() => _result = await From("projects.play", "broken.play");
    [Fact] void should_refuse_compilation() => ShouldRefuse("STAGE-PLAN-003");
    [Fact] void should_preserve_non_blocking_compiler_severity() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0479").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
}
