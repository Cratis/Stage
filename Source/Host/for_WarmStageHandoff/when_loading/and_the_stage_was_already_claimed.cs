// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.when_loading;

public class and_the_stage_was_already_claimed : given.a_warm_stage_handoff
{
    StageHandoffResult _result;

    async Task Establish() => await _handoff.Load(
        new StageLoadRequest(_handoffId, [new StageLoadFile("model.play", "first")]),
        Reset,
        CancellationToken.None);

    async Task Because() => _result = await _handoff.Load(
        new StageLoadRequest(Guid.NewGuid(), [new StageLoadFile("model.play", "second")]),
        Reset,
        CancellationToken.None);

    [Fact] void should_refuse_the_second_handoff() => _result.ShouldEqual(StageHandoffResult.Conflict);
    [Fact] void should_keep_the_first_model() => File.ReadAllText(Path.Combine(_directory, "model.play")).ShouldEqual("first");
    [Fact] void should_only_reset_once() => _resetCount.ShouldEqual(1);
}
