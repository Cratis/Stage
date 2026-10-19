// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_admitting_seed_for_an_event_nothing_produces : Specification
{
    SemanticSeedPlan _seeds = null!;

    void Establish()
    {
        const string Model = """
            module Catalog
              feature Products
                slice StateView Products
                  event ProductListed
                    name String
                  readmodel ProductRow
                    productId String
                    name String
                  projection ProductRows => ProductRow
                    from ProductListed key $eventSourceId
                      productId = $eventSourceId
                      name = name
                  query All => ProductRow[]
            """;
        var plan = compiled_plan.From(Model);
        var syntax = new ScreenplayCompiler().Compile($"{Model}\nseed\n  for \"mug\"\n    ProductListed\n      name = \"Mug\"\n").Value!;
        _seeds = SemanticSeeds.Admit(SemanticSeeds.Create(syntax.Seeds, plan), plan);
    }

    [Fact] void should_seed_it_with_a_plain_text_source() => _seeds.Groups.Single().Facts.Length.ShouldEqual(1);
    [Fact] void should_not_report_anything() => _seeds.Diagnostics.ShouldBeEmpty();
}
