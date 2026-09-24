// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Xunit;

using ContractConstraintViolation = Cratis.Chronicle.Contracts.Events.Constraints.ConstraintViolation;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_checking_the_append_response;

public class and_a_constraint_rejected_the_append : Specification
{
    CommandResult? _result;

    void Because() => _result = ProducedEventAppender.Rejection(new AppendManyResponse
    {
        IsSuccess = false,
        HasConstraintViolations = true,
        ConstraintViolations = [new ContractConstraintViolation { ConstraintName = "UniqueInvoice", Message = "Invoice already exists" }]
    });

    [Fact] void should_not_report_success() => _result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_constraint_message() => _result!.ValidationResults.Single().Message.ShouldEqual("Invoice already exists");
    [Fact] void should_carry_the_constraint_name() => _result!.ValidationResults.Single().ReasonDetail.ShouldEqual("UniqueInvoice");
    [Fact] void should_mark_it_as_a_constraint_failure() => _result!.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConstraintViolation);
}
