// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications;
using Xunit;

using GivenWhenThen = Cratis.Stage.Contracts.Specifications.Specification;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_specification_read_models : given.a_compiled_model_using_2x_constructs
{
    GivenWhenThen _specification = null!;

    void Because() => _specification = _slice.Specifications.Single(specification => specification.Name == "ActivatesAnInvoice");

    [Fact] void should_carry_the_given_read_model() => _specification.GivenReadModels.Count.ShouldEqual(1);
    [Fact] void should_carry_the_then_read_model() => _specification.ThenReadModels.Count.ShouldEqual(1);
    [Fact] void should_name_the_given_read_model() => _specification.GivenReadModels[0].Name.ShouldEqual("InvoiceScope");
    [Fact] void should_carry_the_given_values() => Value(_specification.GivenReadModels[0], "phase").ShouldEqual("Contract");
    [Fact] void should_carry_the_expected_values() => Value(_specification.ThenReadModels[0], "isStarted").ShouldEqual("True");

    // Both steps name the same read model, so both resolve to the same identifier — the way the event and command
    // steps already resolve what they refer to.
    [Fact] void should_resolve_both_steps_to_the_same_read_model() =>
        _specification.ThenReadModels[0].ReadModelId.ShouldEqual(_specification.GivenReadModels[0].ReadModelId);
    [Fact] void should_give_each_step_its_own_identity() =>
        _specification.ThenReadModels[0].Id.ShouldNotEqual(_specification.GivenReadModels[0].Id);

    static string Value(SpecificationReadModel readModel, string property)
    {
        using var values = JsonDocument.Parse(readModel.Values);

        return values.RootElement.GetProperty(property).ToString();
    }
}
