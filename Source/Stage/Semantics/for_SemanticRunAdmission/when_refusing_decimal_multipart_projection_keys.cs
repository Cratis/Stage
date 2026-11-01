// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_refusing_decimal_multipart_projection_keys : Specification
{
    const string Refusal = "Composite key parts must be required, single, non-date, non-decimal primitives or concepts and must not collide with the runtime Id property.";

    string? _oneDecimalPlace;
    string? _twoDecimalPlaces;

    // Chronicle keys composite parts by their text, so 1.0 and 1.00 would split one row in two.
    void Because()
    {
        _oneDecimalPlace = Rejection("1.0");
        _twoDecimalPlaces = Rejection("1.00");
    }

    [Fact] void should_refuse_a_part_with_one_decimal_place() => _oneDecimalPlace.ShouldEqual(Refusal);
    [Fact] void should_refuse_the_same_part_with_two_decimal_places() => _twoDecimalPlaces.ShouldEqual(Refusal);

    static string? Rejection(string price)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DecimalParts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source.Replace("PRICE", price, StringComparison.Ordinal));
        var compilation = new SemanticModelCompiler().Compile("DecimalParts", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        return SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())?.Details;
    }

    const string Source = """
        module Billing
          feature Prices
            slice StateView Tiers
              event PriceListed
                tenant String
                price Decimal
                label String
              readmodel Tier
                tenant String key
                price Decimal key
                label String
              projection Tiers => Tier
                from PriceListed
                  key
                    tenant = tenant
                    price = price
                  label = label
              specification Listed
                given PriceListed
                  tenant = "north"
                  price = PRICE
                  label = "first"
                then readmodel Tier
                  tenant = "north"
                  price = PRICE
                  label = "first"
        """;
}
