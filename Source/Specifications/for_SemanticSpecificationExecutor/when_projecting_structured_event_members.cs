// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_projecting_structured_event_members : Specification
{
    [Theory]
    [InlineData("""address = {"street":"1 Way","city":"Oslo"}""", "Oslo", true)]
    [InlineData("""address = {"street":"1 Way","city":"Oslo"}""", "Bergen", false)]
    public async Task should_agree_with_the_reference_for_whole_and_nested_members(string whole, string city, bool passes)
    {
        var plan = Compile(whole, city);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed == passes, string.Join(';', reference.Failures));
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).ShouldBeNull();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results);
        Assert.True(result.Outcome == (passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed),
            $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    static SemanticExecutionPlan Compile(string whole, string city)
    {
        var source = $$"""
            type Address
              street String
              city   String
            module Shop
              feature Orders
                slice StateView Lookup
                  command Place
                    id      String identifier
                    address Address
                    lines   Address[]
                    produces Placed
                      for id
                      address = address
                      lines   = lines
                  event Placed
                    address Address
                    lines   Address[]
                  readmodel Order
                    id       String key
                    address  Address
                    lines    Address[]
                    city     String
                  projection Orders => Order
                    no automap
                    from Placed
                      id      = $eventSourceId
                      address = address
                      lines   = lines
                      city    = address.city
                  specification Projecting
                    when Place
                      id = "one"
                      {{whole}}
                      lines = [{"street":"2 Way","city":"Trondheim"}]
                    then Placed
                      for "one"
                      {{whole}}
                      lines = [{"street":"2 Way","city":"Trondheim"}]
                    then readmodel Order
                      id      = "one"
                      address = {"street":"1 Way","city":"Oslo"}
                      lines   = [{"street":"2 Way","city":"Trondheim"}]
                      city    = "{{city}}"
            """;
        var syntax = new ScreenplayCompiler().Parse(source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("StructuredMembers"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var bound = new SemanticModelBinder().Bind("StructuredMembers", syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        return SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
    }
}
