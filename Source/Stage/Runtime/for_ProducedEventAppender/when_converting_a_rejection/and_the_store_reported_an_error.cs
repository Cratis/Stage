// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_converting_a_rejection;

public class and_the_store_reported_an_error : Specification
{
    Exception _error = null!;

    void Because() => _error = ProducedEventAppender.ExceptionFor(CommandResult.Error(CorrelationId.New(), "SchemaValidation: Property 'registeredFor' is required."));

    [Fact] void should_be_an_append_rejection() => _error.ShouldBeOfExactType<ProducedEventAppendRejected>();
    [Fact] void should_report_the_reason() => _error.Message.ShouldEqual("SchemaValidation: Property 'registeredFor' is required.");
}
