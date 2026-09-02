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
    IReadOnlyList<string> _compilationErrors = null!;

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_orderSlice, _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_join_the_first_joined_event_onto_the_property_it_feeds() =>
        _file.Content.ShouldContain("[Join<CustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRegistered.Name))]");
    [Fact] void should_join_the_second_joined_event_onto_the_same_property() =>
        _file.Content.ShouldContain(
            "[Join<CustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRegistered.Name))] " +
            "[Join<CustomerRenamed>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRenamed.Name))] string CustomerName");

    [Fact] void should_render_the_all_mapping_as_a_property_targeted_from_all() =>
        _file.Content.ShouldContain("[property: FromAll(contextProperty: \"Occurred\")] DateTimeOffset LastEventAt");
    [Fact] void should_render_the_every_context_mapping_as_from_every() =>
        _file.Content.ShouldContain("[FromEvery(contextProperty: \"Occurred\")] DateTimeOffset LastUpdatedAt");
    [Fact] void should_render_the_every_event_source_id_mapping_as_from_every() =>
        _file.Content.ShouldContain("[FromEvery(contextProperty: \"EventSourceId\")] EventSourceId LastTouchedBy");
    [Fact] void should_declare_the_counted_property_the_all_block_names() => _file.Content.ShouldContain("int TotalEvents");
    [Fact] void should_report_that_the_counter_in_the_all_block_is_not_carried() =>
        _file.Diagnostics.ShouldContain(
            "Mapping of type 'CountMappingSyntax' for 'totalEvents' in an 'all' block has no model-bound equivalent — rendered without a projection attribute.");

    // The mappings of an 'all' block render, but the system-wide subscription it declares cannot be expressed by
    // any model-bound attribute, so the loss has to be reported rather than left to look like it survived.
    [Fact] void should_report_that_the_all_subscription_is_not_carried() =>
        _file.Diagnostics.ShouldContain(
            "An 'all' block subscribes to every event type in the system, which no model-bound attribute expresses — " +
            "its mappings are rendered, but the projection only observes the events its 'from' blocks name.");

    [Fact] void should_render_the_remove_via_join_block_with_its_key() =>
        _file.Content.ShouldContain("[RemovedWithJoin<CustomerAccountClosed>(key: nameof(CustomerAccountClosed.CustomerId))]");

    [Fact] void should_not_flag_any_block_as_unrendered() => _file.Content.ShouldNotContain("TODO:");
    [Fact] void should_render_the_key_the_projection_declares() =>
        _file.Content.ShouldContain("[Key] [SetFrom<OrderPlaced>(nameof(OrderPlaced.OrderNumber))] string OrderNumber");
}
