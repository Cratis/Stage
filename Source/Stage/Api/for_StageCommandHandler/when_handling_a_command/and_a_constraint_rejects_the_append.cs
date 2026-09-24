// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_constraint_rejects_the_append : given.a_command_handler
{
    ProducedEventConstraintRejected _rejection = null!;
    Exception? _error;

    void Establish()
    {
        _rejection = new(ValidationResult.Error("Invoice already exists", reason: ValidationResultReason.ConstraintViolation, reasonDetail: "UniqueInvoice"));
        _appender.Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .ThrowsAsync(_rejection);
    }

    async Task Because() => _error = await Catch.Exception(() => HandlerFor("invoiceId").Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\"}")).AsTask());

    [Fact] void should_fail_with_the_constraint_rejection() => _error.ShouldEqual(_rejection);
    [Fact] void should_fail_as_a_validation_result() => ((IValidationFailure)_error!).ValidationResult.ReasonDetail.ShouldEqual("UniqueInvoice");
}
