// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security;

namespace Cratis.Stage.Contracts.Rendering;

static class ArtifactRenderMarkdown
{
    internal static string Write(ArtifactRenderPlan plan)
    {
        var inventory = plan.Inventory!;
        var lines = new List<string> { "# Unsupported constructs", string.Empty, "Selected scopes:" };
        foreach (var scope in ArtifactRenderReports.Scopes(plan)) lines.Add($"- {scope.Kind} `{scope.Artifact}`");
        lines.Add(string.Empty);
        var score = inventory.Score.BasisPoints is { } value ? $"{value.ToString(CultureInfo.InvariantCulture)} bp" : "not applicable";
        lines.Add($"Outcome: {plan.Outcome.ToCanonicalName()}. Owners: {inventory.Score.Counts.Total.ToString(CultureInfo.InvariantCulture)}. Score: {score}.");
        lines.Add(string.Empty);
        var unresolved = inventory.Constructs.Where(construct => construct.Outcome != ArtifactRenderOutcome.Rendered).ToArray();
        if (unresolved.Length == 0) lines.Add("No degraded or refused constructs in this selection.");
        foreach (var construct in unresolved) WriteConstruct(lines, construct);
        return string.Join('\n', lines).TrimEnd('\n') + "\n";
    }

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
