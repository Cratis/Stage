// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_mismatched_source_key : given.a_captured_delivery
{
    void Establish() => MarkSourceKey();
    void Because() => Render();

    [Theory]
    [InlineData("foreign-stream", "parcel", "CaptureKeyIsNotTheEventSource")]
    [InlineData("PARCEL", "parcel", "CaptureKeyIsNotTheEventSource")]
    [InlineData("parcel", "", "EmptyCaptureKey")]
    public async Task should_refuse_before_reading_the_inbox_or_appending(string source, string key, string exception)
    {
        var error = await Catch.Exception(() => Deliver(Fact(0, source: source, key: key)));
        error!.GetType().Name.ShouldEqual(exception);
        await _inbox.DidNotReceiveWithAnyArgs().GetFromSequenceNumber(default!, default, default);
        _store.DidNotReceiveWithAnyArgs().GetEventSequence(default!);
        _log.Attempts.ShouldBeEmpty();
    }
}
#endif
