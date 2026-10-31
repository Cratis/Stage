// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_disconnecting_client : given.a_live_query
{
    async Task Because()
    {
        await Prepare();
        _stream.Invoke();
        _stream.Subscribe();
        _stream.SendRows();
        _stream.Disconnect();
        _stream.SendRows();
    }

    [Fact] void should_stop_emitting() => _stream.Emissions.Count.ShouldEqual(1);
    [Fact] void should_dispose_upstream() => _stream.Disposals.ShouldEqual(1);
}
#endif
