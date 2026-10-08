// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_unauthenticated_identity_fixtures : a_command_only_plan
{
    SemanticSpecificationRunRecord[] _results = [];

    async Task Because()
    {
        var callers = new SemanticCaller[] { new(false, ["Registrar"], []), new(false, [], [new("department", "Finance")]) };
        var results = new List<SemanticSpecificationRunRecord>();
        foreach (var caller in callers)
        {
            var specification = _specification with { GivenCaller = caller };
            var report = await new SemanticSpecificationExecutor().Run(With(specification), new([specification.Id]), new());
            results.Add(report.Results.Single());
        }
        _results = [.. results];
    }

    [Fact] void should_refuse_both_impossible_guest_fixtures() => _results.Select(result => result.Outcome).ShouldContainOnly([SemanticSpecificationOutcome.Unsupported, SemanticSpecificationOutcome.Unsupported]);
    [Fact] void should_report_authorization_admission() => _results.All(result => result.Unsupported?.Capability == StageExecutionCapability.Authorization).ShouldBeTrue();
    [Fact] void should_name_the_specification() => _results.All(result => result.Unsupported?.Construct == _specification.Id.ToString()).ShouldBeTrue();
    [Fact] void should_explain_the_guest_boundary() => _results.All(result => result.Unsupported?.Details == "An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.").ShouldBeTrue();
    [Fact] void should_execute_nothing() => _results.All(result => result.Trace is null && result.Failures.Count == 0).ShouldBeTrue();
}
