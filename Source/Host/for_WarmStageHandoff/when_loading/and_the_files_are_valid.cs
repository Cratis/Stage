// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.when_loading;

public class and_the_files_are_valid : given.a_warm_stage_handoff
{
    const string Content = "application Test";
    StageHandoffResult _result;

    async Task Because() => _result = await _handoff.Load(
        new StageLoadRequest(_handoffId, [new StageLoadFile("model.play", Content)]),
        Reset,
        CancellationToken.None);

    [Fact] void should_accept_the_handoff() => _result.ShouldEqual(StageHandoffResult.Accepted);
    [Fact] void should_write_the_model() => File.ReadAllText(Path.Combine(_directory, "model.play")).ShouldEqual(Content);
    [Fact] void should_record_the_handoff_identifier() => WarmStageHandoff.ReadHandoffId(_directory).ShouldEqual(_handoffId);
    [Fact] void should_reset_the_kernel() => _resetCount.ShouldEqual(1);
}
