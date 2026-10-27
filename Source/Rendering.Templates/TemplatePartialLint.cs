// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class TemplatePartialLint(LoadedKit kit)
{
    internal static readonly string[] Categories = ["trust", "integrity", "budget", "authorization", "event_stream"];

    internal void Render(ScriptFunctionCall call)
    {
        if (call.Arguments.Count is < 1 or > 2 || call.Arguments[0] is not ScriptLiteral { Value: string name } || !HasPartial(name)) throw Refuse(call, TemplateCodes.Partial, "render requires a literal declared partial name and optional argument.");
    }

    internal void Expression(ScriptFunctionCall call, string helper)
    {
        var count = helper == "lower" ? 4 : 2;
        if (call.Arguments.Count != count || call.Arguments[1] is not ScriptLiteral { Value: string table } || !kit.Manifest.Tables.Any(entry => entry.Name == table)) throw Refuse(call, TemplateCodes.Helper, "Expression requires a literal declared table.");
        if (helper == "lower" && (call.Arguments[2] is not ScriptLiteral { Value: string leaf } || !HasPartial(leaf) || call.Arguments[3] is not ScriptLiteral { Value: string role } || !TemplateRoles.TryParse(role, out _))) throw Refuse(call, TemplateCodes.Helper, "Expression requires a literal leaf partial and role.");
    }

    static TemplateRefusal Refuse(ScriptNode node, string code, string reason) => TemplateRefusal.At(code, reason, node.Span);

    bool HasPartial(string name) => kit.Manifest.Partials.Any(partial => partial.Name == name);
}
