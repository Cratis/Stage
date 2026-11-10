// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_lowering_floating_point_seeds : a_seeded_model
{
    SemanticSeedPlan _seeds = null!;

    void Establish() => Compile(string.Empty);

    void Because()
    {
        var location = new SourceLocation(1, 1);
        SeedEventSyntax Event(object value) => new("ProductListed", [new PropertyMappingSyntax("price", new LiteralExpressionSyntax(value, location), location)], location);
        _seeds = SemanticSeeds.Create([new SeedSyntax([new SeedGroupSyntax("mug", [Event(1.23d), Event(1.23f)], location)], location)], _plan);
    }

    [Fact] void should_keep_the_double_price() => ((SemanticNumberValue)_seeds.Groups.Single().Facts[0].Values.Single().Value).Value.ShouldEqual(1.23m);
    [Fact] void should_keep_the_single_price() => ((SemanticNumberValue)_seeds.Groups.Single().Facts[1].Values.Single().Value).Value.ShouldEqual(1.23m);
}
