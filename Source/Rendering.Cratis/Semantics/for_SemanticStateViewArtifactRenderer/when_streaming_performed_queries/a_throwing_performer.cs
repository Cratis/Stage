// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_throwing_performer : given.a_live_query
{
    Exception _failure = null!;

    async Task Because()
    {
        await Prepare("throw new InvalidOperationException(\"construction failed\");");
        _failure = Catch.Exception(_stream.Invoke);
    }

    [Fact] void should_throw_at_the_query_call() => _failure.InnerException.Message.ShouldEqual("construction failed");
    [Fact] void should_not_subscribe() => _stream.Subscriptions.ShouldEqual(0);
}
#endif
