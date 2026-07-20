// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Rules;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_command : given.a_compiled_invoicing_model
{
    CommandDefinition _command = null!;
    JsonElement _properties;
    IReadOnlyList<string> _required = [];

    void Because()
    {
        _command = _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "RegisterInvoice").Command!;
        using var schema = JsonDocument.Parse(_command.Schema);
        _properties = schema.RootElement.GetProperty("properties").Clone();
        _required = [.. schema.RootElement.GetProperty("required").EnumerateArray().Select(element => element.GetString()!)];
    }

    [Fact] void should_name_the_command() => _command.Name.ShouldEqual("RegisterInvoice");
    [Fact] void should_produce_an_empty_state_schema() => _command.StateSchema.ShouldEqual(SchemaSynthesizerEmptyObject);
    [Fact] void should_synthesize_a_uuid_property_for_the_id() => TypeOf("invoiceId").ShouldEqual("string");
    [Fact] void should_synthesize_a_string_property() => TypeOf("invoiceNumber").ShouldEqual("string");
    [Fact] void should_synthesize_a_number_property_for_a_decimal_concept() => TypeOf("amount").ShouldEqual("number");
    [Fact] void should_synthesize_an_integer_property_for_an_int_concept() => TypeOf("quantity").ShouldEqual("integer");
    [Fact] void should_synthesize_a_property_for_the_optional_field() => TypeOf("note").ShouldEqual("string");
    [Fact] void should_require_the_non_optional_fields() => _required.ShouldContain("invoiceId");
    [Fact] void should_not_require_the_optional_field() => _required.Contains("note").ShouldEqual(false);
    [Fact] void should_group_the_rules_by_property() => _command.Rules.Count.ShouldEqual(2);
    [Fact] void should_map_the_not_empty_rule() => RulesFor("invoiceNumber").OfType<NotEmpty>().Any().ShouldBeTrue();
    [Fact] void should_map_the_matches_rule() => RulesFor("invoiceNumber").OfType<Matches>().Single().Pattern.ShouldEqual("^INV-[0-9]{6}$");
    [Fact] void should_map_the_greater_than_rule() => RulesFor("quantity").OfType<GreaterThan>().Single().Threshold.ShouldEqual(0d);
    [Fact] void should_carry_the_custom_message() => RulesFor("invoiceNumber").OfType<NotEmpty>().Single().ErrorMessage.ShouldEqual("Invoice number is required");

    const string SchemaSynthesizerEmptyObject = """{"type":"object","properties":{}}""";

    string? TypeOf(string property) => _properties.GetProperty(property).GetProperty("type").GetString();

    IReadOnlyList<RuleDefinition> RulesFor(string property) => _command.Rules.Single(group => group.PropertyName == property).Rules;
}
