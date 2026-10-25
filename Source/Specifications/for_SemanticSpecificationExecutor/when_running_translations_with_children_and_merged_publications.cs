// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_translations_with_children_and_merged_publications : Specification
{
    const string Source = """
        type Line
          sku      String
          quantity Int
        module Shop
          feature Sales
            slice Translate Receive
              direction inbound
              event OrderConfirmed from "Sales"
                orderId String
                lines   Line[]
              capture Demand
                source events
                  from OrderConfirmed
                key orderId
                children lines identified by sku
                  append DemandRecorded
                    when added
                      orderId  = $.orderId
                      sku      = $.sku
                      quantity = $.quantity
              event DemandRecorded
                orderId  String
                sku      String
                quantity Int
              specification OneDemandPerLine
                when append OrderConfirmed
                  orderId = "o1"
                  lines   = [{"sku":"A","quantity":3},{"sku":"B","quantity":1}]
                then DemandRecorded
                  for "o1"
                  orderId  = "o1"
                  sku      = "A"
                  quantity = 3
                then DemandRecorded
                  for "o1"
                  orderId  = "o1"
                  sku      = "B"
                  quantity = 1
              specification ARepeatAddsOnlyTheNewLine
                given OrderConfirmed
                  for "o1"
                  orderId = "o1"
                  lines   = [{"sku":"A","quantity":3}]
                when append OrderConfirmed
                  orderId = "o1"
                  lines   = [{"sku":"A","quantity":3},{"sku":"B","quantity":1}]
                then DemandRecorded
                  for "o1"
                  orderId  = "o1"
                  sku      = "B"
                  quantity = 1
        """;

    [Fact]
    public async Task should_run_each_added_child_like_the_reference()
    {
        var plan = Compile(Source);
        foreach (var id in plan.Specifications.Keys)
        {
            var reference = new SemanticSpecificationRunner().Run(plan, id);
            Assert.True(reference.Passed, string.Join(';', reference.Failures));
        }

        var results = (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results;
        results.Count.ShouldEqual(2);
        foreach (var result in results)
        {
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        }
    }

    const string Published = """
        module Shop
          feature Catalog
            slice StateChange Prices
              event Drafted
                sku String
              event PriceSet
                price Int
              command Draft
                id  String identifier
                sku String
                produces Drafted
                  for id
                  sku = sku
              command SetPrice
                id    String identifier
                price Int
                produces PriceSet
                  for id
                  price = price
            slice Translate Publish
              direction outbound
              public event PriceChanged
                productId String
                sku       String
                newPrice  Int
              projection PricePublisher => PriceChanged
                from Drafted
                  productId = $eventSourceId
                  sku       = sku
                from PriceSet
                  newPrice = price
              specification PublishingOnceTheWholeFactIsKnown
                given Drafted
                  for "p1"
                  sku = "A"
                when append PriceSet
                  for "p1"
                  price = 59
                then PriceChanged
                  productId = "p1"
                  sku       = "A"
                  newPrice  = 59
        """;

    [Fact]
    public async Task should_publish_a_public_event_merged_from_several_facts_like_the_reference()
    {
        var plan = Compile(Published);
        var id = plan.Specifications.Keys.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    static SemanticExecutionPlan Compile(string source)
    {
        var syntax = new ScreenplayCompiler().Parse(source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Children"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var bound = new SemanticModelBinder().Bind("Children", syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        return SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
    }
}
