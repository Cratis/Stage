// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor;

public class when_resolving_guard_attachment_contexts : Specification
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData("nested", "Contains", 0, 1, "screens", "item.address.note")]
    [InlineData("nested", "StartsWith", 0, 1, "screens", "item.address.note")]
    [InlineData("row", "Contains", 0, 1, "screens", "item.note")]
    [InlineData("row", "StartsWith", 0, 1, "screens", "item.note")]
    [InlineData("mixed-row", "Contains", 0, 1, "screens", "item.note")]
    [InlineData("template", "Contains", 1, 2, "screenTemplates", "item.note")]
    [InlineData("outlets", "Contains", 1, 2, "screens", "item.note")]
    [InlineData("contribution", "Contains", 0, 1, "screens", "item.note")]
    [InlineData("isolated-contribution", "Equal", 1, 1, "screens", "item.note")]
    [InlineData("reused", "Contains", 1, 2, "screens", "item.note")]
    [InlineData("duplicate-slice", "Contains", 1, 1, "screens", "item.note")]
    [InlineData("duplicate-module", "Contains", 1, 1, "screens", "item.note")]
    [InlineData("qualified", "Contains", 0, 1, "screens", "item.note")]
    [InlineData("number", "StartsWith", 1, 1, "screens", "item.note")]
    [InlineData("enumeration", "Equal", 0, 1, "screens", "item.note")]
    [InlineData("nested-enumeration", "Equal", 0, 1, "screens", "item.address.note")]
    [InlineData("row-enumeration", "Equal", 0, 1, "screens", "item.note")]
    [InlineData("ambiguous", "Equal", 1, 1, "screens", "item.note")]
    [InlineData("unknown", "Equal", 1, 1, "screens", "item.missing")]
    [InlineData("unknown", "NotEqual", 1, 1, "screens", "item.missing")]
    public void should_resolve_each_attachment_without_dropping_or_relocating_guards(string site, string operation, int refused, int count, string root, string subject)
    {
        var scene = new ScreenplaySceneVisitor().Visit(given.guard_context_application.Create(site, Enum.Parse<ComparisonOperator>(operation)));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(scene, _options));
        var guards = Comparisons(json.RootElement, "").ToArray();

        scene.RuntimeIssues.Count.ShouldEqual(refused);
        guards.Length.ShouldEqual(count);
        guards.Count(guard => SceneGuards.CanEvaluate(guard.Condition)).ShouldEqual(count - refused);
        foreach (var guard in guards)
        {
            guard.Location.ShouldContain($"/{root}/");
            guard.Condition.GetProperty("left").GetProperty("subject").GetString().ShouldEqual(subject);
            guard.Condition.GetProperty("right").GetString().ShouldEqual("open");
            if (SceneGuards.CanEvaluate(guard.Condition))
            {
                guard.Condition.GetProperty("operator").GetString().ShouldEqual(operation);
            }
            else
            {
                guard.Condition.GetProperty("operator").GetInt32().ShouldEqual(-1);
            }

            var hasEnumeration = guard.Condition.TryGetProperty("enumeration", out var members);
            if (site.Contains("enumeration", StringComparison.Ordinal))
            {
                hasEnumeration.ShouldBeTrue();
                members.EnumerateArray().Select(member => member.GetString()).ShouldEqual(["open", "closed"]);
            }
            else if (hasEnumeration)
            {
                members.ValueKind.ShouldEqual(JsonValueKind.Null);
            }
        }

        if (new[] { "template", "outlets", "reused" }.Contains(site, StringComparer.Ordinal))
        {
            guards.Count(guard => guard.Location.Contains("first", StringComparison.Ordinal)).ShouldEqual(1);
            guards.Count(guard => guard.Location.Contains("second", StringComparison.Ordinal)).ShouldEqual(1);
        }
    }

    static IEnumerable<(string Location, JsonElement Condition)> Comparisons(JsonElement node, string path)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("componentName", out var component) && component.GetString() == "core:section" &&
                node.TryGetProperty("properties", out var properties) && properties.TryGetProperty("name", out var section))
            {
                path = $"{path}/section:{section.GetString()}";
            }

            if (node.TryGetProperty("kind", out var kind) && kind.GetString() == "comparison") yield return (path, node);
            foreach (var property in node.EnumerateObject())
            {
                foreach (var comparison in Comparisons(property.Value, $"{path}/{property.Name}")) yield return comparison;
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in node.EnumerateArray())
            {
                foreach (var comparison in Comparisons(item, $"{path}/{index}")) yield return comparison;
                index++;
            }
        }
    }
}
