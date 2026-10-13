// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_generated_freshness : Specification
{
    CommandResult _result = null!;

    void Because() => _result = SemanticCommandOutcome.Map(new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Concurrency, null, SemanticGenerationShapes.FreshnessMessage), CorrelationId.NotSet, null, "Create");

    [Fact] void should_refuse_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_preserve_the_freshness_message() => _result.ValidationResults.Single().Message.ShouldEqual(SemanticGenerationShapes.FreshnessMessage);
}
