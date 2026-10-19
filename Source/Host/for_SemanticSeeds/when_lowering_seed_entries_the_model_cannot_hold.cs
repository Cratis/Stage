// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_lowering_seed_entries_the_model_cannot_hold : a_seeded_model
{
    SemanticSeedPlan _seeds = null!;

    void Establish() => Compile("""
        seed
          for "mug"
            ProductListed
              name = "Stoneware mug"
              price = 12.5
            NotAnEvent
              name = "x"
            ProductListed
              nickname = "mug"
          for "tee"
            ProductListed
              name = "Organic cotton tee"
              price = 24
        """);

    void Because() => _seeds = SemanticSeeds.Create(Syntax.Seeds, _plan);

    [Fact] void should_seed_the_entries_it_can_hold() => _seeds.Groups.Select(group => group.Facts.Length).ShouldContainOnly([1, 1]);
    [Fact] void should_report_an_event_that_is_not_in_the_model() => _seeds.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(SemanticSeedPlan.UnresolvedEvent);
    [Fact] void should_report_a_property_the_event_does_not_declare() => _seeds.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(SemanticSeedPlan.UnresolvedProperty);
    [Fact] void should_report_each_left_out_entry_as_a_warning() => _seeds.Diagnostics.All(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeTrue();
}
