// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_lowering_seed_blocks : a_seeded_model
{
    SemanticSeedPlan _seeds = null!;

    void Establish() => Compile("""
        seed
          for "mug"
            ProductListed
              name = "Stoneware mug"
              price = 12.5
              featured = true
              tags = ["kitchen", "gift"]
            ProductListed
              name = "Stoneware mug (large)"
              price = 16
          for "tee"
            ProductListed
              name = "Organic cotton tee"
              price = 24
        seed
          for "mug"
            ProductListed
              name = "Stoneware mug (set)"
              price = 40
        """);

    void Because() => _seeds = SemanticSeeds.Create(Syntax.Seeds, _plan);

    [Fact] void should_keep_one_group_per_event_source_in_declaration_order() => _seeds.Groups.Select(group => group.EventSourceId).ShouldContainOnly(["mug", "tee"]);
    [Fact] void should_accumulate_the_events_of_a_source_across_blocks_in_order() => _seeds.Groups[0].Facts.Length.ShouldEqual(3);
    [Fact] void should_target_the_event_source_id() => ((SemanticTextValue)_seeds.Groups[0].Facts[0].Destination).Value.ShouldEqual("mug");
    [Fact] void should_resolve_the_event_contract() => _plan.Events[_seeds.Groups[0].Facts[0].EventContract].Name.ShouldEqual("ProductListed");
    [Fact] void should_bind_text_values() => Value(0, 0, "name").ShouldEqual(SemanticValue.Text("Stoneware mug"));
    [Fact] void should_bind_number_values() => Value(0, 0, "price").ShouldEqual(SemanticValue.Number(12.5m));
    [Fact] void should_bind_boolean_values() => Value(0, 0, "featured").ShouldEqual(SemanticValue.Boolean(true));
    [Fact] void should_bind_list_values() => ((SemanticArrayValue)Value(0, 0, "tags")).Values.Length.ShouldEqual(2);
    [Fact] void should_only_carry_the_supplied_properties() => _seeds.Groups[0].Facts[1].Values.Length.ShouldEqual(2);
    [Fact] void should_not_report_anything() => _seeds.Diagnostics.ShouldBeEmpty();

    SemanticValue Value(int group, int fact, string property)
    {
        var seeded = _seeds.Groups[group].Facts[fact];
        var id = _plan.Events[seeded.EventContract].Properties.Single(candidate => candidate.Name == property).Id;
        return seeded.Values.Single(value => value.TargetProperty == id).Value;
    }
}
