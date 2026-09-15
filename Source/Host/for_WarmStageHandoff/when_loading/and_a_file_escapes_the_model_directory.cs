// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.when_loading;

public class and_a_file_escapes_the_model_directory : given.a_warm_stage_handoff
{
    StageHandoffResult _result;

    async Task Because() => _result = await _handoff.Load(
        new StageLoadRequest(_handoffId, [new StageLoadFile("../model.play", "invalid")]),
        Reset,
        CancellationToken.None);

    [Fact] void should_reject_the_handoff() => _result.ShouldEqual(StageHandoffResult.InvalidPath);
    [Fact] void should_not_reset_the_kernel() => _resetCount.ShouldEqual(0);
}
