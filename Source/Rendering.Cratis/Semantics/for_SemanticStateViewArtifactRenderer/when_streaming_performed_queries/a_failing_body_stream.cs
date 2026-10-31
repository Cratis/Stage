// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_failing_body_stream : given.a_live_query
{
    async Task Because()
    {
        await Prepare("return readModels.Select<IEnumerable<PersonSummary>, IEnumerable<PersonSummary>>(_ => throw new InvalidOperationException(\"body failed\"));");
        _stream.Invoke();
        _stream.Subscribe();
        _stream.SendRows();
        _stream.SendRows();
    }

    [Fact] void should_deliver_the_body_error() => _stream.Errors.Single().Message.ShouldEqual("body failed");
    [Fact] void should_not_emit() => _stream.Emissions.ShouldBeEmpty();
    [Fact] void should_dispose_upstream() => _stream.Disposals.ShouldEqual(1);
    [Fact] void should_not_resubscribe() => _stream.Subscriptions.ShouldEqual(1);
}
#endif
