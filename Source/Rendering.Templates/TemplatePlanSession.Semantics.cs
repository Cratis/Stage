// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

public sealed partial class TemplatePlanSession
{
    readonly List<TemplateGap> _gaps = [];

    static GapRole ParseRole(string role) => Enum.TryParse<GapRole>(role, false, out var parsed) && Enum.IsDefined(parsed) ? parsed : throw JsonScriptValues.Invalid("Unknown gap role.");

    void InstallSemantics(ScriptObject helpers, ITemplateLanguage language)
    {
        var expressions = new ExpressionLowerer(_prepared.Operators, language, _meter, _budget.MaxRecursion);
        var expressionHelpers = new ScriptObject();
        expressionHelpers.Import("supported", new Func<object?, bool>(value => expressions.Lower(value) is not null));
        expressionHelpers.Import("lower", new Func<object?, SegmentHandle>(value => expressions.Lower(value) ?? MakeGap(language, "STAGE-TPL-EXPR-001", GapRole.ThrowingOperationBody, "Unsupported expression", string.Empty)));
        LanguageHelpers.Set(helpers, "expr", expressionHelpers);
        var types = new ScriptObject();
        types.Import("ref", new Func<object?, SegmentHandle>(value =>
        {
            var descriptor = TypeDescriptors.Parse(value);
            var rendered = language.RenderType(descriptor);
            return rendered is null ? MakeGap(language, "STAGE-TPL-TYPE-001", GapRole.RetainedSignatureOrType, descriptor.Kind, string.Empty) : new([.. rendered]);
        }));
        LanguageHelpers.Set(helpers, "type", types);
        helpers.Import("gap", new Func<string, string, string, string, SegmentHandle>((code, role, intent, path) => MakeGap(language, code, ParseRole(role), intent, path)));
        helpers.Import("refuse", new Func<string, string, object?>((code, message) => throw TemplateRefusal.At(TemplateCodes.Refuse, code + ": " + message, _context.CurrentSpan)));
    }

    SegmentHandle MakeGap(ITemplateLanguage language, string code, GapRole role, string intent, string path)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(intent)) throw JsonScriptValues.Invalid("A gap needs a code and intent.");
        var span = _context.CurrentSpan;
        var gap = new TemplateGap(code, role, intent, path, span.FileName ?? string.Empty, span.Start.Line + 1);
        var emitted = language.EmitGap(gap);
        if (emitted.Length == 0 && role != GapRole.GraphProvenOmittedLeaf) throw JsonScriptValues.Invalid("Language seam emitted an empty unsafe placeholder.");
        _gaps.Add(gap);
        return new([.. emitted]);
    }
}
