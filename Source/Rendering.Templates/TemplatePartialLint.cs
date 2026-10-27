// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class TemplatePartialLint(LoadedKit kit, Dictionary<string, string>? sections)
{
    internal void Render(ScriptFunctionCall call)
    {
        if (call.Arguments.Count is < 1 or > 2) throw Refuse(call, TemplateCodes.Partial, "render requires a literal declared partial name and optional argument.");
        if (call.Arguments[0] is not ScriptLiteral { Value: string name } || !HasPartial(name)) throw Refuse(call, TemplateCodes.Partial, "render requires a literal declared partial name and optional argument.");
    }

    internal void Section(ScriptFunctionCall call)
    {
        if (call.Arguments.Count != 2) throw Refuse(call, TemplateCodes.Helper, "section requires literal section and declared partial names.");
        if (call.Arguments[0] is not ScriptLiteral { Value: string section } || call.Arguments[1] is not ScriptLiteral { Value: string partial }) throw Refuse(call, TemplateCodes.Helper, "section requires literal section and declared partial names.");
        if (!HasPartial(partial)) throw Refuse(call, TemplateCodes.Helper, "section requires literal section and declared partial names.");
        var key = call.Span.FileName + "#" + section;
        CheckDefault(call, key, partial);
        sections?[key] = partial;
    }

    static TemplateRefusal Refuse(ScriptNode node, string code, string reason) => TemplateRefusal.At(code, reason, node.Span);

    void CheckDefault(ScriptFunctionCall call, string key, string partial)
    {
        if (sections is null) return;
        if (!sections.TryGetValue(key, out var previous)) return;
        if (previous != partial) throw Refuse(call, TemplateCodes.Override, "Conflicting section defaults.");
    }

    bool HasPartial(string name) => kit.Manifest.Partials.Any(partial => partial.Name == name);
}
