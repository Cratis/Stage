// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_forwarding_request_cancellation : a_specification_endpoint
{
    readonly CancellationTokenSource _cancellation = new();
    bool _cancelled;
    Exception? _error;
    private protected override ISpecificationRunProcess Process { get; } = Substitute.For<ISpecificationRunProcess>();

    void Establish() => Process.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<CancellationToken>()).Returns(async call =>
    {
        var token = call.Arg<CancellationToken>();
        await _cancellation.CancelAsync();
        _cancelled = token.IsCancellationRequested;
        await Task.FromCanceled(token);
        return new SpecificationProcessResult(0, string.Empty, string.Empty);
    });

    async Task Because() => _error = await Catch.Exception(() => Request("{}", _cancellation.Token));

    [Fact] void should_cancel_the_child_process_token() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_return_a_passed_report() => (_error is OperationCanceledException).ShouldBeTrue();

    void Destroy() => _cancellation.Dispose();
}
