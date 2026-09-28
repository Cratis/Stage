// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_generated_reducer_event_collection_specifications : a_generated_application
{
    string _orderedPath = null!;
    string _unorderedPath = null!;

    const string Source = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                id Uuid identifier
                amounts Decimal[]
                amount Decimal
                produces OrderPlaced
                  for id
                  id = id
                  amounts = amounts
                produces OrderConfirmed
                  for id
                  amount = amount
              event OrderPlaced
                id Uuid
                amounts Decimal[]
              event OrderConfirmed
                amount Decimal
            slice StateView Totals
              readmodel Total
                id Uuid
                amount Decimal
              query ById => Total?
                by id Uuid
              reducer Fold => Total
                on OrderPlaced
                  ```csharp
                  return new Total(context.Event.Id, context.Event.Amounts[0]);
                  ```
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var scenarios = Specification("GoodOrdered", false) + Specification("GoodUnordered", true);
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(Source.Replace("    slice StateView Totals", scenarios + "    slice StateView Totals", StringComparison.Ordinal));
        var plan = when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(source).GetAwaiter().GetResult());
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var ordered = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_good_ordered.cs", StringComparison.Ordinal));
        var unordered = plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_good_unordered.cs", StringComparison.Ordinal));
        _orderedPath = ordered.RelativePath;
        _unorderedPath = unordered.RelativePath;
        Assert.Contains("SequenceEqual(_expected_event_0_amounts)", System.Text.Encoding.UTF8.GetString(ordered.Bytes.AsSpan()), StringComparison.Ordinal);
        Assert.Contains("SequenceEqual(_expected_event_1_amounts)", System.Text.Encoding.UTF8.GetString(unordered.Bytes.AsSpan()), StringComparison.Ordinal);
        return plan;
    }

    static string Specification(string name, bool unordered)
    {
        const string id = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        var lines = new List<string>
        {
            $"      specification {name}",
            "        when PlaceOrder",
            $"          id = \"{id}\"",
            "          amounts = [20, 40]",
            "          amount = 40"
        };
        if (unordered)
        {
            lines.Add("        then events in any order");
            lines.AddRange(["        then OrderConfirmed", "          amount = 40"]);
        }

        lines.AddRange(["        then OrderPlaced", $"          id = \"{id}\"", "          amounts = [20, 40]"]);
        if (!unordered)
        {
            lines.AddRange(["        then OrderConfirmed", "          amount = 40"]);
        }

        return "\n" + string.Join('\n', lines) + "\n";
    }

    void AddMismatchedSpecification(string path, string order)
    {
        var original = ReadGeneratedFile(path);
        var changed = original.Replace($"when_good_{order}", $"when_wrong_{order}", StringComparison.Ordinal)
            .Replace(" = [20m, 40m];", " = [20m, 41m];", StringComparison.Ordinal);
        Assert.Contains(" = [20m, 41m];", changed, StringComparison.Ordinal);
        AddGeneratedSpecification($"Wrong{order}.cs", changed);
    }

    [Fact]
    async Task should_pass_matching_collections_and_reject_mismatched_collections_in_both_event_orders()
    {
        try
        {
            AddMismatchedSpecification(_orderedPath, "ordered");
            AddMismatchedSpecification(_unorderedPath, "unordered");
            var build = await Run("reducer-event-collection-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var passing = await Run("reducer-event-collection-passing.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~when_good_", "--nologo");
            Assert.Contains("Passed!", passing, StringComparison.Ordinal);
            Assert.Contains("Passed:     7", passing, StringComparison.Ordinal);
            foreach (var name in new[] { "wrong_ordered", "wrong_unordered" })
            {
                var failing = await Run($"reducer-event-collection-{name}.log", true, "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName~when_{name}", "--nologo");
                Assert.Contains("Failed!", failing, StringComparison.Ordinal);
                Assert.Contains("Failed:     1", failing, StringComparison.Ordinal);
                Assert.Contains(name == "wrong_ordered" ? "should_have_appended_order_placed_at_position_1" : "should_append_the_expected_event_multiset", failing, StringComparison.Ordinal);
            }
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
