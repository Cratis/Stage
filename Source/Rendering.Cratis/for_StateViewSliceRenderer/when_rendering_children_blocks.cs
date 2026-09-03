// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// A <c>children</c> block renders as a sibling record the parent holds in an <c>IEnumerable&lt;T&gt;</c>, with
/// every projection attribute on that collection property rather than on the child record itself. The compilation
/// is the assertion that matters: only the compiler can say the attributes exist, take the arguments they were
/// given, and are legal where they were placed.
/// </summary>
public class when_rendering_children_blocks : a_projection_with_children_blocks
{
    RenderedFile _file = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_detailsSlice, _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_render_the_child_record_as_a_sibling_type_keyed_on_what_identifies_it() =>
        _file.Content.ShouldContain(
            "public record InvoiceDetailsLineItems([Key] [SetFrom<InvoiceLineItemAdded>(nameof(InvoiceLineItemAdded.LineNumber))] int LineNumber, " +
            "[SetFrom<InvoiceLineItemAdded>(nameof(InvoiceLineItemAdded.Description))] string Description, " +
            "[SetFrom<InvoiceLineItemPriced>(nameof(InvoiceLineItemPriced.UnitPrice))] decimal UnitPrice, ");

    // A child record is driven entirely by the parent's [ChildrenFrom]; a class-level [FromEvent] on it — which is
    // what a nested record carries — would subscribe it as something it is not.
    [Fact] void should_not_declare_the_child_record_as_a_read_model() =>
        _file.Content.ShouldNotContain($"[ReadModel]{Environment.NewLine}public record InvoiceDetailsLineItems");
    [Fact] void should_not_give_the_child_record_a_class_level_subscription() =>
        _file.Content.ShouldNotContain($"[FromEvent<InvoiceLineItemAdded>]{Environment.NewLine}public record InvoiceDetailsLineItems");

    [Fact] void should_hold_the_children_on_an_enumerable_property_of_the_read_model() =>
        _file.Content.ShouldContain("IEnumerable<InvoiceDetailsLineItems> LineItems");
    [Fact] void should_render_the_children_from_attribute_keyed_on_the_event_and_identified_by_the_child() =>
        _file.Content.ShouldContain(
            "[ChildrenFrom<InvoiceLineItemAdded>(key: nameof(InvoiceLineItemAdded.LineNumber), " +
            "identifiedBy: nameof(InvoiceDetailsLineItems.LineNumber))]");

    // [ChildrenFrom] allows multiple, so a children block naming several events renders one attribute per event
    // on the same collection instead of losing everything after the first.
    [Fact] void should_render_one_children_from_attribute_per_event_the_block_names() =>
        _file.Content.ShouldContain(
            "[ChildrenFrom<InvoiceLineItemPriced>(key: nameof(InvoiceLineItemPriced.LineNumber), " +
            "identifiedBy: nameof(InvoiceDetailsLineItems.LineNumber))]");

    // A 'remove with' inside a children block removes one child from the collection, which is a property-level
    // [RemovedWith] — the class-level one it renders to on a read model would remove the whole document.
    [Fact] void should_render_the_removal_on_the_collection_property() =>
        _file.Content.ShouldContain(
            "[RemovedWith<InvoiceLineItemRemoved>(key: nameof(InvoiceLineItemRemoved.LineNumber), " +
            "parentKey: nameof(InvoiceLineItemRemoved.InvoiceNumber))] IEnumerable<InvoiceDetailsLineItems> LineItems");
    [Fact] void should_not_render_the_childs_removal_as_a_class_level_attribute() =>
        _file.Content.ShouldNotContain($"[RemovedWith<InvoiceLineItemRemoved>]{Environment.NewLine}");

    [Fact] void should_render_the_children_of_a_child_as_their_own_sibling_record() =>
        _file.Content.ShouldContain(
            "public record InvoiceDetailsLineItemsAllocations([SetFrom<InvoiceLineAllocationAdded>(" +
            "nameof(InvoiceLineAllocationAdded.Account))] string Account, [Key] int AllocationNumber);");
    [Fact] void should_hold_the_children_of_a_child_on_the_child_record() =>
        _file.Content.ShouldContain(
            "[ChildrenFrom<InvoiceLineAllocationAdded>(key: nameof(InvoiceLineAllocationAdded.AllocationNumber), " +
            "identifiedBy: nameof(InvoiceDetailsLineItemsAllocations.AllocationNumber))] " +
            "IEnumerable<InvoiceDetailsLineItemsAllocations> Allocations");
    [Fact] void should_disable_auto_map_on_the_child_record_that_asks_for_it() =>
        _file.Content.ShouldContain($"[NoAutoMap]{Environment.NewLine}public record InvoiceDetailsLineItemsAllocations");

    // Chronicle identifies a child by a property of the child record, so an 'identified by' that the child's own
    // mappings never produce has to be added rather than referenced into nothing.
    [Fact] void should_report_the_identifying_property_it_had_to_add() =>
        _file.Diagnostics.ShouldContain(
            "The 'identified by' 'allocationNumber' of children record 'InvoiceDetailsLineItemsAllocations' is mapped to no child " +
            "record property — a 'AllocationNumber' property was added to carry it.");

    [Fact] void should_report_the_block_a_child_type_does_not_render() =>
        _file.Diagnostics.ShouldContain(
            "Children record 'InvoiceDetailsLineItems' declares 1 every block(s) whose meaning on a child type is not established — they are not rendered.");
    [Fact] void should_flag_that_block_in_the_file() =>
        _file.Content.ShouldContain("// TODO: 1 every block(s) not yet rendered — their meaning on a child type is not established");

    [Fact] void should_not_report_the_children_blocks_as_unrendered() =>
        _file.Diagnostics.ShouldNotContain(
            "Projection 'InvoiceDetails' declares 1 children block(s) that project into a child record type nothing generates yet — they are not rendered.");
    [Fact] void should_not_leave_a_todo_for_the_children_blocks() =>
        _file.Content.ShouldNotContain("children block(s) not yet rendered");
    [Fact] void should_not_render_a_children_from_without_the_key_the_block_declares() =>
        _file.Content.ShouldNotContain("[ChildrenFrom<InvoiceLineItemAdded>]");
}
