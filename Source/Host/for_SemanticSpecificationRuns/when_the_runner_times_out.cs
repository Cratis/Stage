// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_the_runner_times_out : a_specification_endpoint
{
    private protected override TimeSpan Timeout => TimeSpan.FromMilliseconds(100);
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();

    void Establish() => Process.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).Returns(async call =>
    {
        // Wait for the configured deadline, not a sleep before an assertion.
        await Task.Delay(System.Threading.Timeout.Infinite, call.Arg<CancellationToken>());
        return new SpecificationProcessResult(0, string.Empty, string.Empty);
    });

    async Task Because() => await Request("{}");

    [Fact] void should_return_a_timeout_not_a_pass() => _status.ShouldEqual(StatusCodes.Status504GatewayTimeout);
}
