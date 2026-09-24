// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticCommandOutcome;

public class when_mapping_constraint : Specification
{
    CommandResult _result = null!;

    void Because() => _result = SemanticCommandOutcome.Map(
        new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Constraint, "UniqueProjectName", "Name already exists."),
        CorrelationId.NotSet,
        null,
        "command-1");

    [Fact] void should_return_validation_to_arc() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_the_constraint_name() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("UniqueProjectName");
    [Fact] void should_identify_the_constraint_category() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConstraintViolation);
}
