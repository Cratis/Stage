// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Runtime.for_ProducedEventAppender.when_checking_the_append_response;

public class and_it_succeeded : Specification
{
    void Because() => ProducedEventAppender.Rejection(new AppendManyResponse { IsSuccess = true }).ShouldBeNull();
}
