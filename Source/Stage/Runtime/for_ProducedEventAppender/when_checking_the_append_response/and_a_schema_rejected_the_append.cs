// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Xunit;

using ContractConstraintViolation = Cratis.Chronicle.Contracts.Events.Constraints.ConstraintViolation;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_checking_the_append_response;

public class and_a_schema_rejected_the_append : Specification
{
    CommandResult? _result;

    void Because() => _result = ProducedEventAppender.Rejection(new AppendManyResponse
    {
        IsSuccess = false,
        HasConstraintViolations = true,
        ConstraintViolations = [new ContractConstraintViolation { ConstraintName = "SchemaValidation", Message = "Property 'registeredFor' is required." }]
    });

    [Fact] void should_not_report_success() => _result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_schema_error() => _result!.ExceptionMessages.Single().ShouldEqual("SchemaValidation: Property 'registeredFor' is required.");
}
