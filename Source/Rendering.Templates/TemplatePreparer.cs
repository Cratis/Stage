// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Rendering.Kits;
using Scriban;
using Scriban.Parsing;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Prepares all declared templates and output expressions without executing any of them.</summary>
public static class TemplatePreparer
{
    /// <summary>Checks the templates/v1 contract, pre-scans and parses every declared source.</summary>
    /// <param name="kit">A validated, fully loaded kit.</param>
    /// <returns>The prepared kit, or stable safety refusals.</returns>
    public static TemplateResult<PreparedTemplateKit> Prepare(LoadedKit kit)
    {
        if (!kit.Manifest.Requires.Any(contract => contract.Name == "templates" && contract.Major == 1 && contract.Minor == 0))
        {
            return TemplateResult<PreparedTemplateKit>.Refused(new TemplateDiagnostic(TemplateCodes.MissingContract, "Kit must require templates/v1.0.", string.Empty, 0, 0));
        }

        try
        {
            var templates = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
            var expressions = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
            foreach (var path in kit.Manifest.Outputs.Select(output => output.Template).Concat(kit.Manifest.Partials.Select(partial => partial.Template)).Distinct(StringComparer.Ordinal))
            {
                templates.Add(path, Parse(path, new UTF8Encoding(false, true).GetString(kit.Files[path].AsSpan()), false));
            }

            foreach (var output in kit.Manifest.Outputs)
            {
                expressions.Add(output.Id + ":path", Parse(output.Template + ":path", output.Path, false));
                if (output.When is not null) expressions.Add(output.Id + ":when", Parse(output.Template + ":when", output.When, true));
                if (output.ForEach is not null) expressions.Add(output.Id + ":forEach", Parse(output.Template + ":forEach", output.ForEach, true));
            }

            foreach (var template in templates.Values.Concat(expressions.Values)) new TemplateLint(kit).Visit(template.Page);
            return TemplateResult<PreparedTemplateKit>.Success(new(kit, templates.ToImmutable(), expressions.ToImmutable()));
        }
        catch (TemplateRefusal refusal)
        {
            return TemplateResult<PreparedTemplateKit>.Refused(refusal.Diagnostic);
        }
        catch (DecoderFallbackException)
        {
            return TemplateResult<PreparedTemplateKit>.Refused(new TemplateDiagnostic(TemplateCodes.Parse, "Template is not valid UTF-8.", string.Empty, 0, 0));
        }
    }

    static Template Parse(string path, string text, bool expression)
    {
        TemplatePreScan.Check(path, text, expression);
        var template = Template.Parse(text, path, new ParserOptions { ExpressionDepthLimit = TemplatePreScan.MaxDepth * 2 }, new LexerOptions { Mode = expression ? ScriptMode.ScriptOnly : ScriptMode.Default });
        if (!template.HasErrors) return template;
        var error = template.Messages[0];
        throw TemplateRefusal.At(TemplateCodes.Parse, error.Message, error.Span);
    }
}
