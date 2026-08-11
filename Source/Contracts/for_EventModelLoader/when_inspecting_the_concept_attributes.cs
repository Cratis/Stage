// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// A concept resolves to its underlying primitive in a synthesized schema, which used to erase the compliance
/// markers declared on it entirely — a <c>@pii</c> property arrived indistinguishable from any other string.
/// </summary>
public class when_inspecting_the_concept_attributes : given.a_compiled_model_using_2x_constructs
{
    JsonElement _email;
    JsonElement _account;
    JsonElement _plain;

    void Because()
    {
        using var schema = JsonDocument.Parse(_slice.Command!.Schema);
        var properties = schema.RootElement.GetProperty("properties").Clone();
        _email = properties.GetProperty("email");
        _account = properties.GetProperty("account");
        _plain = properties.GetProperty("invoiceId");
    }

    [Fact] void should_still_resolve_the_concept_to_its_primitive() =>
        _email.GetProperty("type").GetString().ShouldEqual("string");
    [Fact] void should_name_the_concept_the_property_is_typed_as() =>
        _email.GetProperty(SchemaSynthesizer.ConceptKeyword).GetString().ShouldEqual("EmailAddress");
    [Fact] void should_carry_the_pii_marker() =>
        Attributes(_email).EnumerateObject().Select(attribute => attribute.Name).ShouldContain("pii");
    [Fact] void should_carry_the_declared_reason() =>
        Attributes(_email).GetProperty("pii").GetString().ShouldEqual("Billing contact - lawful basis: contract performance");
    [Fact] void should_carry_every_marker_the_concept_declares() =>
        Attributes(_account).EnumerateObject().Select(attribute => attribute.Name).ShouldContainOnly(["pii", "sensitive"]);
    [Fact] void should_leave_a_marker_with_no_reason_empty() =>
        Attributes(_account).GetProperty("pii").GetString().ShouldEqual(string.Empty);
    [Fact] void should_not_annotate_a_concept_that_declares_none() =>
        _plain.TryGetProperty(SchemaSynthesizer.ConceptAttributesKeyword, out _).ShouldBeFalse();

    static JsonElement Attributes(JsonElement property) => property.GetProperty(SchemaSynthesizer.ConceptAttributesKeyword);
}
