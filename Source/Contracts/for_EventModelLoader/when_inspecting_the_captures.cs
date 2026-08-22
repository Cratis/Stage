// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Captures;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// A Translate slice exists to turn an external source into events, and the capture stating which source and
/// which events was dropped - leaving a slice that says it translates and never says from what.
/// </summary>
public class when_inspecting_the_captures : given.a_compiled_model_using_previously_dropped_constructs
{
    CaptureDefinition _capture = null!;

    void Because() => _capture = _legacyInvoiceSync.Captures.Single();

    [Fact] void should_name_the_capture() => _capture.Name.ShouldEqual("LegacyInvoiceCapture");
    [Fact] void should_carry_the_kind_of_source_it_reads() => _capture.Source!.Kind.ShouldEqual("api");
    [Fact] void should_carry_the_settings_of_the_source() =>
        _capture.Source!.Settings.Select(setting => setting.Name).ShouldContainOnly(["route", "poll"]);
    [Fact] void should_carry_the_value_of_a_setting() =>
        _capture.Source!.Settings.Single(setting => setting.Name == "poll").Value.ShouldEqual("5m");
    [Fact] void should_carry_what_identifies_an_instance() => _capture.Key.ShouldEqual("id");

    [Fact] void should_carry_every_mapped_value() => _capture.Map.Count.ShouldEqual(2);
    [Fact] void should_carry_the_property_a_mapping_fills() => Mapping.Property.ShouldEqual("status");
    [Fact] void should_carry_every_value_translation() => Mapping.Translations.Count.ShouldEqual(2);
    [Fact] void should_carry_what_a_value_translates_from() => Mapping.Translations[0].From.ShouldEqual("utkast");
    [Fact] void should_carry_what_a_value_translates_to() => Mapping.Translations[0].To.ShouldEqual("draft");
    [Fact] void should_carry_what_a_split_separates_by() => Split.Separator.ShouldEqual(",");
    [Fact] void should_carry_the_properties_a_split_fills() =>
        Split.Targets.ShouldContainOnly(["firstName", "lastName"]);

    [Fact] void should_carry_every_append() =>
        _capture.Appends.Select(append => append.Event).ShouldContainOnly(["InvoiceStatusChanged", "InvoicePaidFromSent"]);
    [Fact] void should_carry_what_makes_an_append_happen() =>
        Append("InvoiceStatusChanged").When!.Kind.ShouldEqual(CaptureTriggerKind.PropertyChanged);
    [Fact] void should_carry_the_property_a_trigger_watches() =>
        Append("InvoiceStatusChanged").When!.Properties.ShouldContainOnly(["status"]);
    [Fact] void should_carry_the_tags_an_appended_event_gets() =>
        Append("InvoiceStatusChanged").Tags.ShouldContainOnly(["legacy"]);
    [Fact] void should_carry_how_an_appended_event_is_filled() =>
        Append("InvoiceStatusChanged").Mappings.Single().Property.ShouldEqual("invoiceId");
    [Fact] void should_carry_a_value_transition() =>
        Append("InvoicePaidFromSent").When!.Kind.ShouldEqual(CaptureTriggerKind.ValueTransition);
    [Fact] void should_carry_the_value_transitioned_away_from() =>
        Append("InvoicePaidFromSent").When!.FromValue.ShouldEqual("sent");
    [Fact] void should_carry_the_value_transitioned_to() =>
        Append("InvoicePaidFromSent").When!.ToValue.ShouldEqual("paid");

    [Fact] void should_carry_the_child_collection_it_captures() =>
        _capture.Children.Single().Property.ShouldEqual("lineItems");
    [Fact] void should_carry_what_identifies_a_child() =>
        _capture.Children.Single().IdentifiedBy.ShouldEqual("lineNumber");
    [Fact] void should_carry_what_a_child_appends() =>
        _capture.Children.Single().Appends.Single().Event.ShouldEqual("InvoiceLineItemAdded");
    [Fact] void should_carry_the_nested_object_it_captures() =>
        _capture.Nested.Single().Property.ShouldEqual("billingContact");
    [Fact] void should_carry_what_a_nested_object_appends() =>
        _capture.Nested.Single().Appends.Single().Event.ShouldEqual("BillingContactUpdated");

    [Fact] void should_derive_identifiers_deterministically() =>
        EventModelLoader.LoadFromSource(Source)
            .Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "LegacyInvoiceSync")
            .Captures.Single().Id.ShouldEqual(_capture.Id);

    CaptureMapping Mapping => _capture.Map.OfType<CaptureMapping>().Single();

    CaptureSplit Split => _capture.Map.OfType<CaptureSplit>().Single();

    CaptureAppend Append(string name) => _capture.Appends.Single(append => append.Event == name);
}
