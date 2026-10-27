// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Rendering.Kits;
using Scriban;
using Scriban.Parsing;

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplatePreparer
{
    internal static TemplateResult<PreparedTemplateKit> Prepare(LoadedKit kit, ContractOverrides? overrides = null)
    {
        var errors = new List<TemplateDiagnostic>();
        if (!kit.Manifest.Requires.Any(contract => contract.Name == "templates" && contract.Major == 1 && contract.Minor == 0)) errors.Add(new(TemplateCodes.MissingContract, "Kit must require templates/v1.0.", string.Empty, 0, 0));
        var templates = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
        var expressions = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
        var sections = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in kit.Manifest.Outputs.Select(output => output.Template).Concat(kit.Manifest.Partials.Select(partial => partial.Template)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            Attempt(() => templates.Add(path, Parse(path, new UTF8Encoding(false, true).GetString(kit.Files[path].AsSpan()), false)), path, errors);
        }

        foreach (var output in kit.Manifest.Outputs)
        {
            AddExpression("path", output.Path, false);
            if (output.When is not null) AddExpression("when", output.When, true);
            if (output.ForEach is not null) AddExpression("forEach", output.ForEach, true);

            void AddExpression(string kind, string text, bool expression) => Attempt(() => expressions.Add(output.Id + ":" + kind, Parse(output.Template + ":" + kind, text, expression)), output.Template, errors);
        }

        foreach (var template in templates.Values.Concat(expressions.Values).OrderBy(template => template.SourceFilePath, StringComparer.Ordinal)) Attempt(() => new TemplateLint(kit, sections).Visit(template.Page), template.SourceFilePath, errors);
        foreach (var section in overrides?.Sections ?? [])
        {
            if (!sections.ContainsKey(section.Key) || !kit.Manifest.Partials.Any(partial => partial.Name == section.Value)) errors.Add(new(TemplateCodes.Override, "Unknown section or replacement partial: " + section.Key, string.Empty, 0, 0));
            else sections[section.Key] = section.Value;
        }

        var operators = OperatorTable.Empty;
        var operatorSource = kit.Manifest.Tables.FirstOrDefault(table => table.Name == "operators");
        if (operatorSource is not null) Attempt(() => operators = OperatorTable.Parse(kit.Files[operatorSource.Path].AsSpan()), operatorSource.Path, errors);
        return errors.Count > 0 ? TemplateResult<PreparedTemplateKit>.Refused([.. errors]) : TemplateResult<PreparedTemplateKit>.Success(new(kit, templates.ToImmutable(), expressions.ToImmutable(), operators, sections.ToImmutableDictionary(StringComparer.Ordinal)));
    }

    static void Attempt(Action action, string? path, List<TemplateDiagnostic> errors)
    {
        try { action(); }
        catch (TemplateRefusal refusal) { errors.Add(refusal.Diagnostic); }
        catch (Exception error) when (error is DecoderFallbackException or KeyNotFoundException)
        {
            errors.Add(new(TemplateCodes.Parse, "Missing source or invalid UTF-8.", path ?? string.Empty, 0, 0));
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
