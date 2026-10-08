// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_rejecting_too_many_scopes : a_specification_endpoint
{
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();

    async Task Because() => await Request(JsonSerializer.Serialize(new { scopes = Enumerable.Repeat(_plan.Specifications.Keys.Single().ToString(), 1001).ToArray() }));

    [Fact] void should_return_bad_request_even_for_duplicates() => _status.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_not_launch_a_process() => Process.ReceivedCalls().ShouldBeEmpty();
}
