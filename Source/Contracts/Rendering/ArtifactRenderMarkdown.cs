// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderMarkdown
{
    internal static string Write(ArtifactRenderPlan plan)
    {
        var lines = new List<string> { "# Unsupported constructs", string.Empty, "Selected scopes:" };
        foreach (var scope in ArtifactRenderReports.Scopes(plan)) lines.Add($"- {scope.Kind} `{scope.Artifact}`");
        lines.Add(string.Empty);
        WriteInventory(lines, plan);
        WriteExclusions(lines, plan.Exclusions);
        return string.Join('\n', lines).TrimEnd('\n') + "\n";
    }

    static void WriteInventory(List<string> lines, ArtifactRenderPlan plan)
    {
        if (plan.Inventory is not { } inventory)
        {
            lines.Add($"Outcome: {plan.Outcome.ToCanonicalName()}.");
            return;
        }
        var score = inventory.Score.BasisPoints is { } value ? $"{value.ToString(CultureInfo.InvariantCulture)} bp" : "not applicable";
        lines.Add($"Outcome: {plan.Outcome.ToCanonicalName()}. Owners: {inventory.Score.Counts.Total.ToString(CultureInfo.InvariantCulture)}. Score: {score}.");
        lines.Add(string.Empty);
        var unresolved = inventory.Constructs.Where(construct => construct.Outcome != ArtifactRenderOutcome.Rendered).ToArray();
        if (unresolved.Length == 0 && plan.Exclusions.IsEmpty) lines.Add("No degraded or refused constructs in this selection.");
        foreach (var construct in unresolved) WriteConstruct(lines, construct);
    }

    static void WriteExclusions(List<string> lines, IEnumerable<SemanticExclusion> exclusions)
    {
        var values = exclusions.ToArray();
        if (values.Length == 0) return;
        lines.Add(string.Empty);
        lines.Add("## Excluded from the executable model");
        lines.Add("These declarations were excluded before rendering. They are not inventory owners and do not affect the completeness score.");
        foreach (var exclusion in values) WriteExclusion(lines, exclusion);
    }

    static void WriteExclusion(List<string> lines, SemanticExclusion exclusion)
    {
        lines.Add(string.Empty);
        lines.Add($"### {exclusion.Kind}: {ExclusionText(exclusion.Address)}");
        lines.Add($"Source: {Location(exclusion.Location)}. Cause: {exclusion.Cause}.");
        lines.Add(ArtifactRenderExclusions.Remedy);
        if (!string.IsNullOrWhiteSpace(exclusion.Reason)) lines.Add($"Reason: {ExclusionText(exclusion.Reason)}");
        if (!exclusion.DependsOn.IsEmpty) lines.Add("Depends on: " + string.Join(", ", exclusion.DependsOn.Select(dependency => $"{dependency.Kind}: {ExclusionText(dependency.Address)}")));
        foreach (var diagnostic in exclusion.Diagnostics)
            lines.Add($"- {ExclusionText(diagnostic.Code)}: {ExclusionText(diagnostic.Message)} ({Location(diagnostic.Location)})");
    }

    static string Location(SourceLocation location) => $"{ExclusionText(string.IsNullOrWhiteSpace(location.Path) ? "Source unavailable" : location.Path)}:{location.Line.ToString(CultureInfo.InvariantCulture)}:{location.Column.ToString(CultureInfo.InvariantCulture)}";

    static string ExclusionText(string text) => Safe(text).Replace("\u0085", "<br>", StringComparison.Ordinal)
        .Replace("\u2028", "<br>", StringComparison.Ordinal).Replace("\u2029", "<br>", StringComparison.Ordinal)
        .Replace("\\", "&#92;", StringComparison.Ordinal)
        .Replace("[", "&#91;", StringComparison.Ordinal).Replace("]", "&#93;", StringComparison.Ordinal)
        .Replace("*", "&#42;", StringComparison.Ordinal).Replace("_", "&#95;", StringComparison.Ordinal)
        .Replace("|", "&#124;", StringComparison.Ordinal);

    static void WriteConstruct(List<string> lines, ArtifactRenderConstruct construct)
    {
        lines.Add($"## {Source(construct.ModelPath)} — {construct.Kind} `{construct.Identity}`");
        lines.Add($"Semantic locator: {Safe(construct.SemanticLocator.Kind.ToString())} [" + string.Join(", ", construct.SemanticLocator.Parts.Select(part => $"{part.Kind}: {Safe(part.Key)}")) + "]");
        foreach (var gap in construct.Degradations) WriteGap(lines, gap);
        foreach (var reason in construct.RefusalReasons) lines.Add($"- {Safe(reason.Code)}: {Safe(reason.Message)}");
    }

    static void WriteGap(List<string> lines, ArtifactRenderConstructGap gap)
    {
        lines.Add($"- {gap.Evidence.Diagnostic.Code} ({gap.PlaceholderKind}): {Safe(gap.Evidence.Diagnostic.Message)}");
        lines.Add($"  Intent: {Safe(gap.Intent)} Remedy: {Safe(gap.Evidence.Remedy)}");
        if (!gap.Roots.IsEmpty) lines.Add("  Causes: " + string.Join(", ", gap.Roots.Select(root => $"`{root.Owner}/{root.Code}`")));
    }

    static string Source(string? path) => path is null ? "Source unavailable" : Safe(path);

    static string Safe(string text) => (SecurityElement.Escape(text) ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n').Replace("\n", "<br>", StringComparison.Ordinal).Replace("`", "&#96;", StringComparison.Ordinal);
}
