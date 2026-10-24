// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_child_projections : Specification
{
    [Theory]
    [InlineData("append", true)]
    [InlineData("update", true)]
    [InlineData("implicit-identity", true)]
    [InlineData("remove", true)]
    [InlineData("join", true)]
    [InlineData("root-join", true)]
    [InlineData("parent-property", true)]
    [InlineData("missing-optional", true)]
    [InlineData("child-id", true)]
    [InlineData("wrong", false)]
    [InlineData("reordered", false)]
    [InlineData("missing-parent", false)]
    [InlineData("missing-parent-remove", false)]
    public async Task should_match_ordered_reference_children(string action, bool passes)
    {
        var plan = Compile(action);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        Assert.True(reference.Passed == passes, string.Join(';', reference.Failures));
        var executor = new SemanticSpecificationExecutor();
        for (var run = 0; run < 2; run++)
        {
            var result = Assert.Single((await executor.Run(plan, new([]), new())).Results);
            Assert.True(result.Outcome == (passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed),
                $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)} {string.Join(';', result.Trace?.ReadModels.Values ?? [])}");
        }
    }

    [Fact]
    public async Task should_refuse_a_child_before_its_later_parent_by_name()
    {
        var plan = Compile("late-parent");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.Details.ShouldContain("existing parent instance");
    }

    [Fact]
    public async Task should_refuse_an_optional_child_mapping_source_by_name()
    {
        var plan = Compile("optional-source");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.Details.ShouldContain("optional child source or null literal");
    }

    [Theory]
    [InlineData("join")]
    [InlineData("root-join")]
    public async Task should_refuse_a_join_whose_event_identity_has_another_value_type(string action)
    {
        var plan = Compile(action, numberedLabels: true);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())!.Details.ShouldEqual(
            "A join's event-source identity must have the joined key's value type and enumeration values.");
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.ShouldEqual("A join's event-source identity must have the joined key's value type and enumeration values.");
    }

    [Fact]
    public void should_keep_product_arithmetic_refused_by_name()
    {
        var plan = Compile("product");
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single()).Details.ShouldContain("product");
    }

    [Fact]
    public void should_observe_events_in_child_and_join_scopes_recursively()
    {
        var plan = Compile("update");
        var projection = plan.Projections.Values.Single();
        foreach (var @event in plan.Events.Values)
            SemanticProjectionObservation.Observes(projection, @event.Id).ShouldBeTrue();
        var scope = projection.Scope;
        var child = scope.Children.Single();
        var nested = projection with { Scope = scope with { From = [], Children = [child with { Scope = child.Scope with { From = [], Joins = [], Removals = [], Children = [child] } }] } };
        var placed = plan.Events.Values.Single(@event => @event.Name == "Placed").Id;
        var opened = plan.Events.Values.Single(@event => @event.Name == "Opened").Id;
        SemanticProjectionObservation.Observes(nested, placed).ShouldBeTrue();
        SemanticProjectionObservation.Observes(nested, opened).ShouldBeFalse();
    }

    static SemanticExecutionPlan Compile(string action, bool numberedLabels = false)
    {
        var operation = action switch
        {
            "root-join" => "when append Labeled\n  for \"parent\"\n  label = \"renamed\"",
            "remove" or "missing-parent-remove" => "when append Removed\n  for \"parent\"\n  code = \"second\"",
            "join" => "when append Labeled\n  for \"second\"\n  label = \"renamed\"",
            _ => $"when append Placed\n  for \"parent\"\n  code = \"{(action == "append" ? "third" : "second")}\"\n  quantity = 3"
        };
        var expected = action switch
        {
            "missing-optional" => /*lang=json*/ "[{\"code\":\"second\",\"quantity\":3},{\"code\":\"first\",\"quantity\":1}]",
            "append" => /*lang=json*/ "[{\"code\":\"second\",\"quantity\":2,\"label\":\"original\"},{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"},{\"code\":\"third\",\"quantity\":3,\"label\":\"third label\"}]",
            "remove" => /*lang=json*/ "[{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"}]",
            "root-join" => /*lang=json*/ "[{\"code\":\"second\",\"quantity\":2,\"label\":\"original\"},{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"}]",
            "join" => /*lang=json*/ "[{\"code\":\"second\",\"quantity\":2,\"label\":\"renamed\"},{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"}]",
            "reordered" => /*lang=json*/ "[{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"},{\"code\":\"second\",\"quantity\":3,\"label\":\"original\"}]",
            _ => $"[{{\"label\":\"original\",\"quantity\":{(action == "wrong" ? 99 : 3)},\"code\":\"second\"}},{{\"code\":\"first\",\"quantity\":1,\"label\":\"first label\"}}]"
        };
        var source = $$"""
            type Entry
              code String
              quantity Int
              label String optional
            module Work
              feature Work
                slice StateView Lookup
                  event Opened
                    name String
                  event Placed
                    code String
                    quantity Int
                  event Removed
                    code String
                  event Labeled
                    label String
                  command RecordOpened
                    source String identifier
                    name String
                    produces Opened
                      for source
                      name = name
                  command RecordPlaced
                    source String identifier
                    code String
                    quantity Int
                    produces Placed
                      for source
                      code = code
                      quantity = quantity
                  command RecordRemoved
                    source String identifier
                    code String
                    produces Removed
                      for source
                      code = code
                  command RecordLabeled
                    source String identifier
                    label String
                    produces Labeled
                      for source
                      label = label
                  readmodel Current
                    id String key
                    name String
                    entries Entry[]
                  projection Build => Current
                    no automap
                    from Opened
                      name = name
                    children entries identified by code
                      from Placed key code
                        parent $eventSourceId
                        code = code
            {{(action == "product" ? "            add quantity by quantity * quantity" : "            quantity = quantity")}}
                      remove with Removed key code
                        parent $eventSourceId
                      join labels on code
                        with Labeled
                          label = label
                  specification Children
                    given Opened
                      for "parent"
                      name = "parent name"
                    given Labeled
                      for "second"
                      label = "original"
                    given Labeled
                      for "first"
                      label = "first label"
                    given Labeled
                      for "third"
                      label = "third label"
                    given Placed
                      for "parent"
                      code = "second"
                      quantity = 2
                    given Placed
                      for "parent"
                      code = "first"
                      quantity = 1
            {{string.Join('\n', operation.Split('\n').Select(line => "        " + line))}}
                    then readmodel Current
                      id = "parent"
                      name = "parent name"
                      entries = {{expected}}
            """;
        if (action == "root-join")
        {
            source = source.Replace("        children entries", "        join parentLabel on id\n          with Labeled\n            name = label\n        children entries", StringComparison.Ordinal)
                .Replace("        then readmodel Current\n          id = \"parent\"\n          name = \"parent name\"", "        then readmodel Current\n          id = \"parent\"\n          name = \"renamed\"", StringComparison.Ordinal);
        }
        if (action == "parent-property")
        {
            source = source.Replace("      event Placed\n        code String\n        quantity Int", "      event Placed\n        code String\n        quantity Int\n        parentId String", StringComparison.Ordinal)
                .Replace("        produces Placed\n          for source", "        produces Placed\n          for source\n          parentId = \"parent\"", StringComparison.Ordinal)
                .Replace("        given Placed\n          for \"parent\"", "        given Placed\n          for \"parent\"\n          parentId = \"parent\"", StringComparison.Ordinal)
                .Replace("        when append Placed\n          for \"parent\"", "        when append Placed\n          for \"unrelated-source\"\n          parentId = \"parent\"", StringComparison.Ordinal)
                .Replace("            parent $eventSourceId", "            parent parentId", StringComparison.Ordinal);

            // Only Placed carries parentId; the removal retains its event-source parent.
            source = source.Replace("          remove with Removed key code\n            parent parentId", "          remove with Removed key code\n            parent $eventSourceId", StringComparison.Ordinal);
        }
        if (numberedLabels)
        {
            // Labeled's event source becomes a number while the joined child code and root id stay text.
            source = source.Replace("      command RecordLabeled\n        source String identifier", "      command RecordLabeled\n        source Int identifier", StringComparison.Ordinal);
            foreach (var (code, number) in new[] { ("second", 2), ("first", 1), ("third", 3), ("parent", 7) })
                source = source.Replace($"Labeled\n          for \"{code}\"", $"Labeled\n          for {number}", StringComparison.Ordinal);
        }
        if (action == "child-id") source = source.Replace("code", "id", StringComparison.Ordinal);
        if (action == "optional-source") source = source.Replace("      event Labeled\n        label String", "      event Labeled\n        label String optional", StringComparison.Ordinal);
        if (action == "missing-optional")
        {
            foreach (var (code, label) in new[] { ("second", "original"), ("first", "first label"), ("third", "third label") })
                source = source.Replace($"        given Labeled\n          for \"{code}\"\n          label = \"{label}\"\n", string.Empty, StringComparison.Ordinal);
        }
        if (action == "implicit-identity") source = source.Replace("            code = code\n", string.Empty, StringComparison.Ordinal);
        if (action is "missing-parent" or "missing-parent-remove") source = source.Replace("        given Opened\n          for \"parent\"\n          name = \"parent name\"\n", string.Empty, StringComparison.Ordinal);
        if (action == "late-parent")
        {
            const string opened = "        given Opened\n          for \"parent\"\n          name = \"parent name\"\n";
            const string firstPlaced = "          code = \"first\"\n          quantity = 1\n";
            source = source.Replace(opened, string.Empty, StringComparison.Ordinal).Replace(firstPlaced, firstPlaced + opened, StringComparison.Ordinal);
        }
        return when_running_property_keyed_removal_only_projections.Compile(source);
    }
}
