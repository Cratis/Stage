// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_rendering_every_model_bound_block : a_projection_using_every_model_bound_block
{
    RenderedFile _file = null!;

    void Because() => _file = new StateViewSliceRenderer().Render(_orderSlice, _applicationSet, "CratisApp");

    [Fact] void should_join_the_first_event() =>
        _file.Content.ShouldContain("[Join<CustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRegistered.Name))]");
    [Fact] void should_join_the_second_event_to_the_same_property() =>
        _file.Content.ShouldContain("[Join<CustomerRenamed>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRenamed.Name))] string CustomerName");
    [Fact] void should_map_every_event_context() =>
        _file.Content.ShouldContain("[FromEvery(contextProperty: \"Occurred\")] DateTimeOffset LastUpdatedAt");
    [Fact] void should_map_every_event_source_id() =>
        _file.Content.ShouldContain("[FromEvery(contextProperty: \"EventSourceId\")] EventSourceId LastTouchedBy");
    [Fact] void should_render_the_keyed_remove_via_join() =>
        _file.Content.ShouldContain("[RemovedWithJoin<CustomerAccountClosed>(key: nameof(CustomerAccountClosed.CustomerId))]");
    [Fact] void should_render_the_projection_key() =>
        _file.Content.ShouldContain("[Key] [SetFrom<OrderPlaced>(nameof(OrderPlaced.OrderNumber))] string OrderNumber");
    [Fact] void should_compile_the_supported_output() => RenderedOutput.Errors([_file]).ShouldBeEmpty();
}
