// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_role_and_claim_only_policies : a_command_only_plan
{
    readonly List<(bool Allowed, SemanticSpecificationRunRecord Result)> _results = [];

    async Task Because()
    {
        var original = _plan.Commands[_specification.When!.Command];
        var policies = new SemanticPolicy[]
        {
            new("RoleOnly", new SemanticRoleCondition("Registrar")),
            new("ClaimOnly", new SemanticClaimCondition("department", SemanticClaimTargetKind.Literal, "Finance"))
        };
        foreach (var policy in policies)
        {
            var command = original with { Authorization = new SemanticPolicyReference(policy.Name) };
            var matching = policy.Name == "RoleOnly"
                ? new SemanticCaller(true, ["Registrar"], [])
                : new SemanticCaller(true, [], [new("DEPARTMENT", "Other"), new("department", "Finance")]);
            var callers = new (SemanticCaller Caller, bool Allowed)[] { (matching, true), (new(true, [], []), false), (new(false, [], []), false) };
            foreach (var (caller, allowed) in callers)
            {
                var specification = _specification with { GivenCaller = caller, ThenDenied = !allowed, ThenEvents = allowed ? _specification.ThenEvents : [] };
                var report = await new SemanticSpecificationExecutor().Run(WithBehavior(specification, command, policy: policy), new([specification.Id]), new());
                _results.Add((allowed, report.Results.Single()));
            }
        }
    }

    [Fact] void should_pass_every_allow_and_deny_vector() => (_results.Count == 6 && _results.TrueForAll(vector => vector.Result.Outcome == SemanticSpecificationOutcome.Passed)).ShouldBeTrue();
    [Fact] void should_append_only_for_matching_authenticated_callers() => _results.TrueForAll(vector => vector.Result.Trace is { } trace && trace.Facts.Count == (vector.Allowed ? _specification.ThenEvents.Length : 0)).ShouldBeTrue();
}
