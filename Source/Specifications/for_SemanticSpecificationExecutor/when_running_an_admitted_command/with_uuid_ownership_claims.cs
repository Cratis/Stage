// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_uuid_ownership_claims : a_command_only_plan
{
    SemanticSpecificationRunReport _matching = null!;
    SemanticSpecificationRunReport _different = null!;
    SemanticSpecificationRunReport _malformed = null!;
    SemanticSpecificationRunReport _missing = null!;
    SemanticSpecificationRunReport _repeated = null!;

    async Task Because()
    {
        var original = _plan.Commands[_specification.When!.Command];
        var identifier = original.Properties.Single(property => property.IsIdentifier);
        var subject = ((SemanticTextValue)_specification.When.Values.Single(value => value.TargetProperty == identifier.Id).Value).Value;
        Guid.TryParse(subject, out var uuid).ShouldBeTrue();
        var policy = new SemanticPolicy("Owns", new SemanticLogicalPolicyCondition(
            new SemanticClaimCondition("owner", SemanticClaimTargetKind.Subject, null),
            SemanticLogicalOperator.And,
            new SemanticClaimCondition("owner", SemanticClaimTargetKind.Artifact, identifier.Name)));
        var command = original with { Authorization = new SemanticPolicyReference(policy.Name) };
        async Task<SemanticSpecificationRunReport> Run(bool allowed, params string[] claims)
        {
            var specification = _specification with
            {
                GivenCaller = new SemanticCaller(true, [], [.. claims.Select(value => new SemanticCaller(true, [], [new("OWNER", value)]).Claims[0])]),
                ThenDenied = !allowed,
                ThenEvents = allowed ? _specification.ThenEvents : []
            };
            return await new SemanticSpecificationExecutor().Run(WithBehavior(specification, command, policy: policy), new([specification.Id]), new());
        }

        _matching = await Run(true, uuid.ToString("B").ToUpperInvariant());
        _different = await Run(false, Guid.NewGuid().ToString());
        _malformed = await Run(false, "not-a-uuid");
        _missing = await Run(false);
        _repeated = await Run(true, "not-a-uuid", subject);
    }

    [Fact] void should_allow_a_typed_uuid_match() => _matching.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_deny_a_different_uuid() => _different.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_deny_a_malformed_claim() => _malformed.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_deny_a_missing_claim() => _missing.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_allow_a_repeated_claim_with_one_match() => _repeated.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
