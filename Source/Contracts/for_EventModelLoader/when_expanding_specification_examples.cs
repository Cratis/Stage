// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_expanding_specification_examples : Specification
{
    const string Source = """
        module Billing
          feature Invoicing
            slice StateChange Recording
              command Record
                id String identifier
                amount Int
              event Recorded
                amount Int
              readmodel Balance
                id String identifier
                amount Int
              query BalanceById => Balance?
                by id String
              example Input : Record
                id = "invoice-1"
                amount = 10
              example Fact : Recorded
                amount = 10
              example View : Balance
                id = "invoice-1"
                amount = 10
              specification ChangingTheAmount
                given Fact amount = 20
                given readmodel View
                when Input amount = 20
                then Fact
                  amount = 20
                then readmodel View
                  amount = 20
        """;

    Slice _slice = null!;
    Specifications.Specification _specification = null!;

    void Because()
    {
        _slice = EventModelLoader.LoadFromSource(Source).Collections.Single().Modules.Single().Features.Single().Slices.Single();
        _specification = _slice.Specifications.Single();
    }

    [Fact] void should_resolve_the_command_name() => _specification.When!.Name.ShouldEqual("Record");
    [Fact] void should_resolve_the_command_identity() => _specification.When!.CommandId.ShouldEqual(_slice.Command!.Id);
    [Fact] void should_inherit_the_command_identity_value() => Value(_specification.When!.Values, "id").GetString().ShouldEqual("invoice-1");
    [Fact] void should_apply_the_inline_override() => Value(_specification.When!.Values, "amount").GetInt32().ShouldEqual(20);
    [Fact] void should_resolve_given_event_identity() => _specification.Given.Single().EventId.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_expand_given_event_values() => Value(_specification.Given.Single().Values, "amount").GetInt32().ShouldEqual(20);
    [Fact] void should_resolve_then_event_identity() => _specification.ThenEvents.Single().EventId.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_apply_the_indented_override() => Value(_specification.ThenEvents.Single().Values, "amount").GetInt32().ShouldEqual(20);
    [Fact] void should_resolve_given_read_model_identity() => _specification.GivenReadModels.Single().ReadModelId.ShouldEqual(_slice.ReadModel!.Id);
    [Fact] void should_inherit_given_read_model_values() => Value(_specification.GivenReadModels.Single().Values, "amount").GetInt32().ShouldEqual(10);
    [Fact] void should_resolve_then_read_model_identity() => _specification.ThenReadModels.Single().ReadModelId.ShouldEqual(_slice.ReadModel!.Id);
    [Fact] void should_override_then_read_model_values() => Value(_specification.ThenReadModels.Single().Values, "amount").GetInt32().ShouldEqual(20);

    static JsonElement Value(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(property).Clone();
    }
}
