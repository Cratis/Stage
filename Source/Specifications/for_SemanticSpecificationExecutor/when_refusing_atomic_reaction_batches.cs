// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_atomic_reaction_batches : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V23, SemanticVersion.V23, model.Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic }), []).Plan!;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_examine_every_specification() => _report.Results.Count.ShouldEqual(7);
    [Fact] void should_never_execute_with_legacy_prefix_semantics() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_atomic_production_boundary() => _report.Results.ShouldEachConformTo(result => result.Unsupported!.Details.StartsWith("STAGE-ESM-043:", StringComparison.Ordinal) && result.Unsupported.Details.Contains("atomic", StringComparison.Ordinal));
}
