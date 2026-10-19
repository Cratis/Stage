// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_an_unaudited_version : a_command_only_plan
{
    readonly List<SemanticSpecificationRunReport> _reports = [];

    async Task Because()
    {
        // Simulate dependency versions Stage has not audited.
        foreach (var version in new uint[] { 31, 32 })
        {
            typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new LanguageVersion(version, 0));
            typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new SemanticVersion(version, 0));
            _reports.Add(await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new()));
        }
    }

    [Fact] void should_refuse_instead_of_executing() => _reports.ShouldEachConformTo(report => report.Results.Single().Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_stage_version_gate() => _reports.ShouldEachConformTo(report => report.Results.Single().Unsupported!.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
}
