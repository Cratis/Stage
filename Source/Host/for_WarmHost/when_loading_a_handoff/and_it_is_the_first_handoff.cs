// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmHost.when_loading_a_handoff;

public class and_it_is_the_first_handoff : for_WarmStageHandoff.given.a_warm_stage_handoff
{
    IResult _result = null!;
    int _acceptedCount;

    async Task Because() => _result = await WarmHost.Load(
        _handoff,
        new StageLoadRequest(_handoffId, [new StageLoadFile("model.play", "application Test")]),
        Reset,
        () => _acceptedCount++,
        CancellationToken.None);

    [Fact] void should_answer_accepted() => ((IStatusCodeHttpResult)_result).StatusCode.ShouldEqual(StatusCodes.Status202Accepted);
    [Fact] void should_restart_against_the_model_once() => _acceptedCount.ShouldEqual(1);
    [Fact] void should_reset_the_kernel() => _resetCount.ShouldEqual(1);
}
