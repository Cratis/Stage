// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_with_malformed_scope : a_specification_endpoint
{
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();

    async Task Because() => await Request("{\"scopes\":[\"not-an-id\"]}");

    [Fact] void should_return_bad_request() => _status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_not_run_any_specifications() => Process.ReceivedCalls().ShouldBeEmpty();
}
