// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_structure : given.a_compiled_invoicing_model
{
    [Fact] void should_name_the_model_after_the_module() => _model.Name.ShouldEqual("Invoicing");
    [Fact] void should_wrap_the_modules_in_a_single_collection() => _model.Collections.Count.ShouldEqual(1);
    [Fact] void should_link_the_collection_to_the_model() => _model.Collections[0].EventModelId.ShouldEqual(_model.Id);
    [Fact] void should_have_the_one_module() => _model.Collections[0].Modules.Count.ShouldEqual(1);
    [Fact] void should_name_the_module() => Module.Name.ShouldEqual("Invoicing");
    [Fact] void should_link_the_module_to_the_model() => Module.EventModelId.ShouldEqual(_model.Id);
    [Fact] void should_have_the_one_feature() => Module.Features.Count.ShouldEqual(1);
    [Fact] void should_name_the_feature() => Feature.Name.ShouldEqual("InvoiceManagement");
    [Fact] void should_have_a_top_level_feature() => Feature.ParentFeatureId.ShouldBeNull();
    [Fact] void should_have_the_three_slices() => Feature.Slices.Count.ShouldEqual(3);
    [Fact] void should_map_the_state_change_slice() => Slice("RegisterInvoice").SliceType.ShouldEqual(SliceType.StateChange);
    [Fact] void should_map_the_state_view_slice() => Slice("InvoiceList").SliceType.ShouldEqual(SliceType.StateView);
    [Fact] void should_map_the_automation_slice() => Slice("NotifyOnRegistered").SliceType.ShouldEqual(SliceType.Automation);
    [Fact] void should_derive_identifiers_deterministically() => EventModelLoader.LoadFromSource(Source).Id.ShouldEqual(_model.Id);

    Module Module => _model.Collections[0].Modules[0];

    Feature Feature => Module.Features[0];

    Slice Slice(string name) => Feature.Slices.Single(slice => slice.Name == name);
}
