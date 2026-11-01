// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Cratis.Stage.Rendering.Templates;

internal static class HelperReference
{
    internal static string Generate() => """
        ---
        title: Template helpers
        description: Frozen stage-templates/v1 helpers, examples, safety codes and budgets.
        ---

        <!-- Generated, do not edit. Source: HelperCatalogue.GenerateReference(). -->
        <!-- Copyright (c) Cratis. All rights reserved. -->
        <!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

        The `stage-templates/v1` profile is the locked, target-neutral interpreter contract for declarative renderer kits.
        Examples use the test language pack, not a production target. Vectors marked with operators use the test
        operator table and a declared `leaf` partial that emits a `Domain` symbol reference for `item.identifier`.
        Outputs show JSON-quoted text, ordered segments or a safety refusal; gaps are recorded alongside text.
        Helper bytes, signatures and semantics are frozen once released. Change them under a new helper name or `templates/v2`.
        See [Author a renderer kit](../guides/template-kits.md) for scope, symbols and caller safety obligations.

        """ + "\n" + string.Join('\n', HelperCatalogue.All.OrderBy(helper => helper.Name, StringComparer.Ordinal).Select(Describe)) + Codes() + Allowlist() + Budgets();

    static string Describe(HelperDescriptor helper) => "## " + helper.Name + "\n\n" + Code(helper.Signature) + " — since " + helper.Since + ".\n\n" + helper.Summary + "\n\n| Vector | Template | Scope | Expected output |\n| --- | --- | --- | --- |\n" + string.Join('\n', helper.Vectors.Select(vector => "| " + Code(vector.Id + (vector.Operators ? " (operators)" : string.Empty)) + " | " + Code(vector.Template) + " | " + Code(vector.Scope.GetRawText()) + " | " + Code(Expected(vector)) + " |")) + "\n";

    static string Expected(HelperVector vector)
    {
        if (vector.PrepareRefusal is not null) return "prepare refusal: " + vector.PrepareRefusal;
        if (vector.Refusal is not null) return "render refusal: " + vector.Refusal;
        var output = vector.Segments.IsDefault ? JsonSerializer.Serialize(vector.Text) : JsonSerializer.Serialize(vector.Segments);
        return output + (vector.Gap is null ? string.Empty : "; gap: " + vector.Gap);
    }

    static string Codes() => "\n## Safety refusal codes\n\nAll STAGE-TPL codes are whole-plan safety refusals. Discard every output of a refused plan, including earlier artifacts.\n\n| Code | TemplateCodes constant |\n| --- | --- |\n" + string.Join('\n', typeof(TemplateCodes).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal).Select(field => "| " + Code((string)field.GetRawConstantValue()!) + " | " + Code(field.Name) + " |")) + "\n";

    static string Allowlist() => "\n## Allowed standard library\n\n" + string.Join('\n', LockedBuiltins.Members.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => "- " + Code(entry.Key) + ": " + string.Join(", ", entry.Value.Order(StringComparer.Ordinal).Select(Code)) + ".")) + "\n\nEverything else is unavailable, including date/time, random, environment, regex, dynamic evaluation and JSON parsing.\n";

    static string Budgets()
    {
        var budget = TemplateBudget.Default;
        (string Name, long Value)[] limits =
        [
            ("Template UTF-8 bytes (pre-scan)", TemplateProfile.MaxTemplateBytes),
            ("Template nesting (pre-scan)", TemplateProfile.MaxDepth),
            ("Aggregate steps", budget.MaxSteps),
            ("Aggregate output UTF-8 bytes", budget.MaxOutputBytes),
            ("Artifacts", budget.MaxArtifacts),
            ("Fan-out", budget.MaxFanOut),
            ("String length", budget.MaxStringLength),
            ("Recursion", budget.MaxRecursion)
        ];
        return "\n## Budget defaults\n\nLimits are aggregate across a plan session; all budget values must be positive.\n\n| Limit | Default |\n| --- | --- |\n" + string.Join('\n', limits.Select(limit => "| " + limit.Name + " | " + limit.Value.ToString(CultureInfo.InvariantCulture) + " |")) + "\n| Deadline (seconds) | " + budget.Deadline.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " |\n\nThe caller owns the process-level CPU and memory sandbox; interpreter budgets are not a replacement.\n";
    }

    static string Code(string value)
    {
        var delimiter = value.Contains('`', StringComparison.Ordinal) ? "``" : "`";
        return delimiter + " " + value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal) + " " + delimiter;
    }
}
