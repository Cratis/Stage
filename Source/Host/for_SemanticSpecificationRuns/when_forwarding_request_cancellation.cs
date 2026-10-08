// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Cratis.Stage.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_forwarding_request_cancellation : a_specification_endpoint
{
    readonly CancellationTokenSource _cancellation = new();
    CancellationToken _forwarded;
    protected override ISemanticSpecificationExecutor Executor { get; } = Substitute.For<ISemanticSpecificationExecutor>();

    void Establish() => Executor.Run(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticSpecificationSelection>(), Arg.Any<SemanticSpecificationRunOptions>(), Arg.Any<CancellationToken>())
        .Returns(call =>
        {
            _forwarded = call.Arg<CancellationToken>();
            return new SemanticSpecificationRunReport("stage-spec-run/1", _plan.Model.Application.Id.ToString(), _plan.Revision.ToString(), []);
        });

    async Task Because() => await Request("{}", _cancellation.Token);

    [Fact] void should_forward_the_request_token() => _forwarded.ShouldEqual(_cancellation.Token);

    void Destroy() => _cancellation.Dispose();
}
