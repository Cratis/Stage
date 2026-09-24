// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_converting_a_rejection;

public class and_a_constraint_rejected_it : Specification
{
    Exception _error = null!;

    void Because() => _error = ProducedEventAppender.ExceptionFor(new CommandResult
    {
        ValidationResults = [ValidationResult.Error("Invoice already exists", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueInvoice")]
    });

    [Fact] void should_be_a_constraint_rejection() => _error.ShouldBeOfExactType<ProducedEventConstraintRejected>();
    [Fact] void should_carry_the_message() => ((IValidationFailure)_error).ValidationResult.Message.ShouldEqual("Invoice already exists");
    [Fact] void should_carry_the_constraint_name() => ((IValidationFailure)_error).ValidationResult.ReasonDetail.ShouldEqual("UniqueInvoice");
}
