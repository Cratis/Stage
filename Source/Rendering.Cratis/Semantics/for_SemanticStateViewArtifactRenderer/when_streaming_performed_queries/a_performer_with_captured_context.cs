// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_performer_with_captured_context : given.a_live_query
{
    async Task Because()
    {
        await Prepare("return readModels.Select(rows => rows.Where(row => row.Name == context.Arguments.Name && context.Tenant == \"tenant-a\" && context.Identity.Identity!.AuthenticationType == \"test\" && context.ReceivedAt.Year == 2026));");
        _stream.Invoke();
        ChangeInvocationContext();
        _stream.Subscribe();
        _stream.SendRows();
        _stream.SendRows();
    }

    [Fact] void should_keep_the_invocation_context_for_both_batches() => _stream.Emissions.Select(batch => batch.Length).ShouldEqual([1, 1]);
}
#endif
