// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

// The reference needs an allocated identity once validation passes; this engine allocates none, so it must say so
// rather than fail on the missing destination.
public class with_an_unallocated_destination_and_an_expected_rejection : a_command_only_plan
{
    SemanticSpecificationRunReport _report = null!;

    protected override CanonicalCorpusVector Corpus => RegisterProjectCorpus.LegacyV1;

    async Task Because()
    {
        var command = _plan.Commands[_specification.When!.Command];
        var unallocated = command with { Destination = null, Produces = [.. command.Produces.Select(produced => produced with { Destination = null })] };
        var rejection = _specification with { ThenEvents = [], ThenErrors = [new(null, "Project name is required")] };
        _report = await new SemanticSpecificationExecutor().Run(WithBehavior(rejection, unallocated), new([rejection.Id]), new());
    }

    [Fact] void should_not_claim_success_or_failure() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_identity_allocation() => _report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.IdentityAllocation);
}
