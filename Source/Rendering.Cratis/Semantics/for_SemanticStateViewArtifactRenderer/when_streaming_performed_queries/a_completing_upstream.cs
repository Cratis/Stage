// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_completing_upstream : given.a_live_query
{
    async Task Because()
    {
        await Prepare();
        _stream.Invoke();
        _stream.Subscribe();
        _stream.Complete();
        _stream.SendRows();
    }

    [Fact] void should_deliver_completion() => _stream.Completed.ShouldBeTrue();
    [Fact] void should_not_emit_after_completion() => _stream.Emissions.ShouldBeEmpty();
    [Fact] void should_dispose_upstream() => _stream.Disposals.ShouldEqual(1);
    [Fact] void should_not_resubscribe() => _stream.Subscriptions.ShouldEqual(1);
}
#endif
