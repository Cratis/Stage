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
    internal static TemplatePreparation Prepare(LoadedKit kit, TemplateSectionOverrides? overrides = null)
    {
        var errors = new List<TemplateDiagnostic>();
        if (!kit.Manifest.Requires.Any(contract => contract.Name == "templates" && contract.Major == 1)) errors.Add(new(TemplateCodes.MissingContract, "Kit must require templates/v1.x.", string.Empty, 0, 0));
        var templates = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
        var expressions = ImmutableDictionary.CreateBuilder<string, Template>(StringComparer.Ordinal);
        var sections = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(StringComparer.Ordinal);
        var replacements = ImmutableDictionary.CreateBuilder<(string Template, string Section), Template>();
        var operators = ImmutableDictionary.CreateBuilder<string, OperatorTable>(StringComparer.Ordinal);
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

        foreach (var template in templates.Values.Concat(expressions.Values).OrderBy(template => template.SourceFilePath, StringComparer.Ordinal))
        {
            var lint = new TemplateLint(kit, errors);
            lint.Visit(template.Page);
            if (templates.ContainsKey(template.SourceFilePath!)) sections[template.SourceFilePath!] = [.. lint.Sections];
        }

        foreach (var body in overrides?.Bodies ?? new Dictionary<(string, string), string>())
        {
            var (path, section) = body.Key;
            if (!sections.TryGetValue(path, out var names) || !names.Contains(section, StringComparer.Ordinal))
            {
                errors.Add(new(TemplateCodes.Override, "Unknown template or section: " + section, path, 0, 0));
            }
            else
            {
                Attempt(
                    () =>
                    {
                        var replacement = Parse(path, body.Value, false);
                        new TemplateLint(kit, errors, true).Visit(replacement.Page);
                        replacements.Add(body.Key, replacement);
                    },
                    path,
                    errors);
            }
        }

        foreach (var table in kit.Manifest.Tables) Attempt(() => operators.Add(table.Name, OperatorTable.Parse(kit.Files[table.Path].AsSpan())), table.Path, errors);
        var diagnostics = errors.OrderBy(error => error.Code, StringComparer.Ordinal).ThenBy(error => error.Path, StringComparer.Ordinal).ThenBy(error => error.Line).ThenBy(error => error.Message, StringComparer.Ordinal).ToImmutableArray();
        return new(diagnostics.IsEmpty ? new(kit, templates.ToImmutable(), expressions.ToImmutable(), operators.ToImmutable(), replacements.ToImmutable(), sections.ToImmutable()) : null, diagnostics);
    }

    static void Attempt(Action action, string? path, List<TemplateDiagnostic> errors)
    {
        try { action(); }
        catch (TemplateRefusal refusal) { errors.Add(refusal.Diagnostic with { Path = path ?? string.Empty }); }
        catch (Exception error) when (error is DecoderFallbackException or KeyNotFoundException)
        {
            errors.Add(new(TemplateCodes.Parse, "Missing source or invalid UTF-8.", path ?? string.Empty, 0, 0));
        }
    }

    static Template Parse(string path, string text, bool expression)
    {
        TemplatePreScan.Check(path, text);
        var template = Template.Parse(text, path, new ParserOptions { ExpressionDepthLimit = 64 }, new LexerOptions { Mode = expression ? ScriptMode.ScriptOnly : ScriptMode.Default });
        if (template.HasErrors)
        {
            var error = template.Messages.FirstOrDefault(message => TemplateNesting.IsParserLimit(message.Message)) ?? template.Messages[0];
            throw TemplateNesting.ParserFailure(error);
        }

        TemplateNesting.Check(template.Page);
        return template;
    }
}
