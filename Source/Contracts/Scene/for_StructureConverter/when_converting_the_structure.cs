// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Structure;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StructureConverter;

public class when_converting_the_structure : given.a_translated_application
{
    ModuleNode Shop => _application.Structure.Modules.Single(module => module.Id == "Shop");

    FeatureNode Orders => Shop.Features.Single(feature => feature.Id == "Orders");

    void Because() => Translate(Model);

    [Fact] void should_have_every_module() => _application.Structure.Modules.Select(module => module.Id).ShouldContainOnly("Shop", "Support", "Empty");
    [Fact] void should_have_every_feature_of_a_module() => Shop.Features.Select(feature => feature.Id).ShouldContainOnly("Orders", "Returns");
    [Fact] void should_have_every_slice_of_a_feature() => Orders.Slices.Select(slice => slice.Id).ShouldContainOnly("PlaceOrder", "OrderList", "Notify");
    [Fact] void should_map_a_state_change_slice() => Orders.Slices.Single(slice => slice.Id == "PlaceOrder").Kind.ShouldEqual(SliceKind.StateChange);
    [Fact] void should_map_a_state_view_slice() => Orders.Slices.Single(slice => slice.Id == "OrderList").Kind.ShouldEqual(SliceKind.StateView);
    [Fact] void should_map_an_automation_slice() => Orders.Slices.Single(slice => slice.Id == "Notify").Kind.ShouldEqual(SliceKind.Automation);
    [Fact] void should_carry_the_command_of_a_state_change_slice() => Orders.Slices.Single(slice => slice.Id == "PlaceOrder").Command.ShouldEqual("PlaceOrder");
    [Fact] void should_leave_a_slice_without_a_command_with_none() => Orders.Slices.Single(slice => slice.Id == "OrderList").Command.ShouldBeNull();
    [Fact] void should_leave_a_module_without_features_empty() => _application.Structure.Modules.Single(module => module.Id == "Empty").Features.ShouldBeEmpty();
    [Fact] void should_resolve_a_slice_suffix_to_its_qualified_id() => StructureConverter.QualifiedSlice(_application.Structure, "Orders.PlaceOrder").ShouldEqual("Shop.Orders.PlaceOrder");
    [Fact] void should_resolve_a_bare_unambiguous_slice_name() => StructureConverter.QualifiedSlice(_application.Structure, "StartReturn").ShouldEqual("Shop.Returns.StartReturn");
    [Fact] void should_keep_an_unknown_slice_as_written() => StructureConverter.QualifiedSlice(_application.Structure, "Ghost").ShouldEqual("Ghost");
    [Fact] void should_keep_the_reference_as_written_without_a_structure() => StructureConverter.QualifiedSlice(null, "Orders.PlaceOrder").ShouldEqual("Orders.PlaceOrder");
}
