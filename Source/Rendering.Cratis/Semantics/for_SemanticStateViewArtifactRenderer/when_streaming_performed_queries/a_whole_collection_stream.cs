// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_whole_collection_stream : given.a_live_query
{
    async Task Because()
    {
        await Prepare();
        _stream.Invoke();
        _stream.SendRows();
        _stream.Subscribe();
        _stream.SendRows();
        _stream.SendRows();
    }

    [Fact] void should_deliver_the_initial_collection_and_every_update() => _stream.Emissions.Select(batch => batch.Length).ShouldEqual([2, 2, 2]);
}
#endif
