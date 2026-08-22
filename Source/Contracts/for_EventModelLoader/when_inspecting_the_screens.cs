// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Screens;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// A screen states which read model a slice shows and which commands it offers, and both were dropped on
/// import - so nothing in the model said a command was reachable from anywhere.
/// </summary>
public class when_inspecting_the_screens : given.a_compiled_model_using_previously_dropped_constructs
{
    [Fact] void should_carry_every_declared_screen() =>
        _invoiceList.Screens.Select(screen => screen.Name)
            .ShouldContainOnly(["InvoiceListScreen", "InvoiceDetailScreen", "ExternalScreen"]);

    [Fact] void should_carry_the_read_model_a_screen_shows() =>
        Screen("InvoiceListScreen").Data.Single().ReadModel.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_carry_the_query_the_data_comes_through() =>
        Screen("InvoiceListScreen").Data.Single().Query.ShouldEqual("ListInvoices");
    [Fact] void should_leave_an_unkeyed_binding_unkeyed() =>
        Screen("InvoiceListScreen").Data.Single().By.ShouldBeNull();

    [Fact] void should_carry_the_command_a_screen_offers() =>
        Screen("InvoiceListScreen").Actions.Single().Command.ShouldEqual("RegisterInvoice");
    [Fact] void should_carry_where_an_action_navigates() =>
        Screen("InvoiceListScreen").Actions.Single().NavigatesTo.ShouldEqual("InvoiceDetailScreen");
    [Fact] void should_carry_the_label_of_an_action() =>
        Screen("InvoiceListScreen").Actions.Single().Label.ShouldEqual("Register");

    [Fact] void should_reach_a_binding_nested_inside_a_template_slot() =>
        Screen("InvoiceDetailScreen").Data.Single().Query.ShouldEqual("GetInvoice");
    [Fact] void should_carry_the_parameter_a_nested_binding_is_keyed_by() =>
        Screen("InvoiceDetailScreen").Data.Single().By.ShouldEqual("invoiceId");
    [Fact] void should_reach_an_action_nested_inside_a_section() =>
        Screen("InvoiceDetailScreen").Actions.Single().Command.ShouldEqual("CancelInvoice");

    [Fact] void should_point_at_the_file_a_screen_lives_in() =>
        Screen("ExternalScreen").File.ShouldEqual("Screens/External.tsx");
    [Fact] void should_point_at_no_file_for_an_inline_screen() => Screen("InvoiceListScreen").File.ShouldBeEmpty();
    [Fact] void should_bind_nothing_for_a_file_backed_screen() => Screen("ExternalScreen").Data.ShouldBeEmpty();

    [Fact] void should_derive_identifiers_deterministically() =>
        EventModelLoader.LoadFromSource(Source)
            .Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "InvoiceList")
            .Screens[0].Id.ShouldEqual(_invoiceList.Screens[0].Id);

    ScreenDefinition Screen(string name) => _invoiceList.Screens.Single(screen => screen.Name == name);
}
