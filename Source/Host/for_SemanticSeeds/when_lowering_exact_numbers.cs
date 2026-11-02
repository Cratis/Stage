// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_lowering_exact_numbers : a_seeded_model
{
    SemanticSeedPlan _seeds = null!;

    void Establish() => Compile(string.Empty);

    void Because()
    {
        var location = new SourceLocation(1, 1);
        SeedEventSyntax Event(string number) => new("ProductListed", [new PropertyMappingSyntax("price", new LiteralExpressionSyntax(Exact(number), location), location)], location);
        _seeds = SemanticSeeds.Create([new SeedSyntax([new SeedGroupSyntax("mug", [Event("12.50"), Event("24.0")], location)], location)], _plan);
    }

    static ExactNumber Exact(string text)
    {
        Assert.True(ExactNumber.TryParse(text, out var number));
        return number;
    }

    [Fact] void should_keep_the_fraction() => ((SemanticNumberValue)_seeds.Groups.Single().Facts[0].Values.Single().Value).Value.ToString(System.Globalization.CultureInfo.InvariantCulture).ShouldEqual("12.5");
    [Fact] void should_keep_a_whole_number_whole() => ((SemanticNumberValue)_seeds.Groups.Single().Facts[1].Values.Single().Value).Value.ToString(System.Globalization.CultureInfo.InvariantCulture).ShouldEqual("24");
}
