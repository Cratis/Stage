// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

public sealed partial class TemplatePlanSession
{
    readonly List<TemplateGap> _gaps = [];

    void InstallSemantics(ScriptObject helpers, ITemplateLanguage language, ITypeDirectory directory)
    {
        var expressions = new ScriptObject();
        ExpressionLowerer Lowerer(string table) => _prepared.Operators.TryGetValue(table, out var operators) ? new(operators, language, _meter, _budget.MaxRecursion) : throw JsonScriptValues.Invalid("Unknown operator table.");
        expressions.Import("supported", new Func<object?, string, bool>((value, table) => Lowerer(table).Supported(ExpressionLowerer.Input(value))));
        expressions.Import("lower", new Func<object?, string, string, string, object?>((value, table, leaf, role) =>
        {
            var lowerer = Lowerer(table);
            var input = ExpressionLowerer.Input(value);
            if (!lowerer.Supported(input)) WriteText(RecordGap(language, TemplateRoles.Parse(role), "Unsupported expression", string.Empty, "STAGE-DEGRADE-001"));
            else lowerer.Write(input, WriteText, node => RenderPartial(leaf, JsonScriptValues.Convert(node, _budget.MaxStringLength)));
            return null;
        }));
        LanguageHelpers.Set(helpers, "expr", expressions);
        var types = new ScriptObject();
        types.Import("ref", new Func<object?, SegmentHandle>(value => new(TypeDescriptors.Spell(value, language, directory).Parts)));
        LanguageHelpers.Set(helpers, "type", types);
        object? Gap(string role, string intent, string path, string code = "STAGE-DEGRADE-002")
        {
            if (!new[] { "STAGE-DEGRADE-002", "STAGE-DEGRADE-003", "STAGE-DEGRADE-005" }.Contains(code, StringComparer.Ordinal)) throw JsonScriptValues.Invalid("Gap code must be 002, 003 or 005.");
            WriteText(RecordGap(language, TemplateRoles.Parse(role), intent, path, code));
            return null;
        }

        helpers.Import("gap", new Func<string, string, string, string, object?>(Gap));
        helpers.Import("refuse", new Func<string, string, object?>((category, message) => throw TemplateRefusal.At(TemplateCodes.Refuse, category + ": " + message[..Math.Min(message.Length, 512)], _context.CurrentSpan)));
    }

    string RecordGap(ITemplateLanguage language, GapRole role, string intent, string path, string code)
    {
        if (!_context.AllowStructuredOutput) throw JsonScriptValues.Invalid("A path cannot contain gaps.");
        if (intent.Length is 0 or > 512 || intent.Contains('\n') || intent.Contains('\r') || path.Length > 1024) throw JsonScriptValues.Invalid("Gap intent must be one line of at most 512 characters; path at most 1024.");
        var span = _context.CurrentSpan;
        var emitted = language.Placeholder(role, code, intent);
        if (emitted.Length == 0 && role != GapRole.GraphProvenOmittedLeaf) throw JsonScriptValues.Invalid("Language seam emitted an empty unsafe placeholder.");
        _gaps.Add(new(code, role, intent, path, span.FileName ?? string.Empty, span.Start.Line + 1));
        return emitted;
    }

    void WriteText(string text)
    {
        if (text.Length > _budget.MaxStringLength) throw PlanMeter.Exceeded(TemplateCodes.StringLength, "Helper string exceeds the string limit.");
        if (TemplatePreScan.HasPrivateUse(text)) throw JsonScriptValues.Invalid("Helpers cannot emit private-use text.");
        _context.Write(_context.CurrentSpan, text);
    }
}
