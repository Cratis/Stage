// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Concepts;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// A concept reached a consumer only as an annotation on the properties typed as one, so a concept nothing used
/// arrived nowhere and the values of an enumeration arrived only where some property happened to reference it.
/// </summary>
public class when_inspecting_the_concepts : given.a_compiled_model_using_previously_dropped_constructs
{
    [Fact] void should_carry_every_declared_concept() =>
        _model.Concepts.Select(concept => concept.Name)
            .ShouldContainOnly(["InvoiceId", "CustomerId", "EmailAddress", "InvoiceStatus"]);

    [Fact] void should_carry_the_underlying_type() => Concept("InvoiceId").Type.ShouldEqual("Uuid");
    [Fact] void should_not_call_a_primitive_concept_an_enumeration() => Concept("InvoiceId").IsEnum.ShouldBeFalse();
    [Fact] void should_leave_a_primitive_concept_without_values() => Concept("InvoiceId").Values.ShouldBeEmpty();
    [Fact] void should_call_an_enumeration_one() => Concept("InvoiceStatus").IsEnum.ShouldBeTrue();
    [Fact] void should_carry_the_values_of_an_enumeration() =>
        Concept("InvoiceStatus").Values.ShouldContainOnly(["draft", "sent", "paid"]);

    [Fact] void should_carry_the_compliance_marker() =>
        Concept("EmailAddress").Attributes.Select(attribute => attribute.Name).ShouldContainOnly(["pii"]);
    [Fact] void should_carry_the_reason_declared_with_it() =>
        Concept("EmailAddress").Attributes[0].Reason.ShouldEqual("Billing contact - lawful basis: contract performance");
    [Fact] void should_leave_a_concept_declaring_no_marker_without_any() => Concept("InvoiceId").Attributes.ShouldBeEmpty();

    [Fact] void should_derive_identifiers_deterministically() =>
        EventModelLoader.LoadFromSource(Source).Concepts[0].Id.ShouldEqual(_model.Concepts[0].Id);
    [Fact] void should_give_every_concept_its_own_identifier() =>
        _model.Concepts.Select(concept => concept.Id).Distinct().Count().ShouldEqual(4);

    ConceptDefinition Concept(string name) => _model.Concepts.Single(concept => concept.Name == name);
}
