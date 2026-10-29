// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class TemplateSectionLint(bool isOverride)
{
    readonly List<string> _names = [];
    bool _active;

    internal string[] Names => [.. _names];

    internal void Visit(ScriptWrapStatement wrap, Action<ScriptNode?> visit)
    {
        var name = Name(wrap);
        if (isOverride) throw Invalid(wrap);
        if (_active) throw Invalid(wrap);
        if (_names.Contains(name, StringComparer.Ordinal)) throw Invalid(wrap);
        _names.Add(name);
        _active = true;
        try { visit(wrap.Body); }
        finally { _active = false; }
    }

    static TemplateRefusal Invalid(ScriptWrapStatement wrap) => TemplateRefusal.At(TemplateCodes.Helper, "Invalid, duplicate or nested section.", wrap.Span);

    static string Name(ScriptWrapStatement wrap)
    {
        if (wrap.Target is not ScriptFunctionCall { Target: ScriptVariableGlobal { Name: "section" }, Arguments.Count: 1 } call) throw Invalid(wrap);
        if (call.Arguments[0] is not ScriptLiteral { Value: string name } || !ValidName(name)) throw Invalid(wrap);
        return name;
    }

    static bool ValidName(string name) => name.Length is > 0 and <= 64 && name[0] is >= 'a' and <= 'z' && name.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
