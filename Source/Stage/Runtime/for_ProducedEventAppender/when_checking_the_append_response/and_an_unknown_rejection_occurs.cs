// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_checking_the_append_response;

public class and_an_unknown_rejection_occurs : Specification
{
    CommandResult? _result;

    void Because() => _result = ProducedEventAppender.Rejection(new AppendManyResponse { IsSuccess = false });

    [Fact] void should_not_report_success() => _result!.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_an_error() => _result!.ExceptionMessages.Single().ShouldEqual("Chronicle rejected the produced events without a reason.");
}
