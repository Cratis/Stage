// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_transforming_performer : given.a_live_query
{
    async Task Because()
    {
        await Prepare("return readModels.Select(rows => rows.Where(row => row.Name == name).OrderBy(row => row.Name).Select(row => row with { Name = row.Name + \"!\" }));");
        _stream.Invoke();
        _stream.Subscribe();
        _stream.SendRows();
        _stream.SendRows();
    }

    [Fact] void should_transform_every_batch() => _stream.Emissions.Select(batch => batch.GetValue(0).GetType().GetProperty("Name").GetValue(batch.GetValue(0))).ShouldEqual(["Ada!", "Ada!"]);
    [Fact] void should_subscribe_only_once() => _stream.Subscriptions.ShouldEqual(1);
}
#endif
