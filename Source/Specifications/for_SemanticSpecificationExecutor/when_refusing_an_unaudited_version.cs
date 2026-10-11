// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_an_unaudited_version : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        // Simulate a future dependency admitting a version Stage has not audited.
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new LanguageVersion(10, 0));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new SemanticVersion(10, 0));
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new());

    [Fact] void should_refuse_instead_of_executing() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_stage_version_gate() => _report.Results.Single().Unsupported!.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal).ShouldBeTrue();
}
