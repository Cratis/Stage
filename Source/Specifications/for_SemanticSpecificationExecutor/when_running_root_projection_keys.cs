// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_root_projection_keys : Specification
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    public async Task should_route_keys_and_replace_seeded_history(bool constant, bool seeded, bool command)
    {
        var plan = Compile(constant, seeded, command: command);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        for (var run = 0; run < 2; run++)
        {
            var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
                $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            result.Trace!.ReadModels.Count.ShouldEqual(constant ? 1 : 2);
        }
    }

    [Theory]
    [InlineData("increment count", 3)]
    [InlineData("decrement count", -3)]
    public async Task should_run_optional_counters_from_an_unset_value(string mapping, int expected)
    {
        var plan = Compile(true, false, mapping, expected);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    }

    static SemanticExecutionPlan Compile(bool constant, bool seeded, string? counter = null, int? expected = null, bool command = false)
    {
        var key = constant ? "\"global\"" : "sku";
        var total = seeded ? 14 : 9;
        if (!constant && !seeded) total = 6;
        var source = $$"""
            module Stock
              feature Stock
                slice StateChange Recording
                  command Record
                    id String identifier
                    sku String
                    quantity Int
                    produces Recorded
                      for id
                      sku = sku
                      quantity = quantity
                  event Recorded
                    sku String
                    quantity Int
                  specification RoutesKeys
                    given Recorded
                      for "first"
                      sku = "global"
                      quantity = 2
                    given Recorded
                      for "second"
                      sku = "other"
                      quantity = 3
            {{(seeded ? "        given readmodel Totals\n          id = \"global\"\n          count = 10" : string.Empty)}}
                    when {{(command ? "Record" : "append Recorded")}}
            {{(command ? "          id = \"third\"" : "          for \"third\"")}}
                      sku = "global"
                      quantity = 4
            {{(command ? "        then Recorded\n          for \"third\"\n          sku = \"global\"\n          quantity = 4" : string.Empty)}}
                    then readmodel Totals
                      id = "global"
                      count = {{expected ?? total}}
            {{(!constant ? "        then readmodel Totals\n          id = \"other\"\n          count = 3" : string.Empty)}}
                slice StateView Totals
                  readmodel Totals
                    id String key
                    count Int{{(counter is null ? string.Empty : " optional")}}
                  projection Build => Totals
                    no automap
                    from Recorded key {{key}}
                      id = {{key}}
                      {{counter ?? "add count by quantity"}}
            """;
        return when_running_property_keyed_removal_only_projections.Compile(source);
    }
}
