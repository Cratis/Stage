// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmHost.when_loading_a_handoff;

public class and_the_stage_was_already_claimed : for_WarmStageHandoff.given.a_warm_stage_handoff
{
    IResult _result = null!;
    int _acceptedCount;

    async Task Establish() => await Load(_handoffId, "first");

    async Task Because() => _result = await Load(Guid.NewGuid(), "second");

    [Fact] void should_answer_conflict() => ((IStatusCodeHttpResult)_result).StatusCode.ShouldEqual(StatusCodes.Status409Conflict);
    [Fact] void should_only_restart_against_the_first_model() => _acceptedCount.ShouldEqual(1);
    [Fact] void should_only_reset_the_kernel_once() => _resetCount.ShouldEqual(1);

    Task<IResult> Load(Guid handoffId, string content) => WarmHost.Load(
        _handoff,
        new StageLoadRequest(handoffId, [new StageLoadFile("model.play", content)]),
        Reset,
        () => _acceptedCount++,
        CancellationToken.None);
}
