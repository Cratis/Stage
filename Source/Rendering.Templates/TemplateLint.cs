// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class TemplateLint(LoadedKit kit) : ScriptVisitor
{
    static readonly HashSet<string> _roots = new(["model", "item", "for", "while", "empty", "blank"], StringComparer.Ordinal);
    static readonly Dictionary<string, string[]> _helpers = new(StringComparer.Ordinal)
    {
        ["name"] = ["words", "pascal", "camel", "snake", "kebab", "constant"],
        ["lit"] = ["string", "integer", "decimal", "boolean", "null", "comment", "identifier"],
        ["sym"] = ["decl", "ref", "imports"],
        ["type"] = ["ref"],
        ["expr"] = ["supported", "lower"]
    };
    static readonly HashSet<string> _calls = new(["render", "section", "refuse", "gap"], StringComparer.Ordinal);
    readonly HashSet<string> _locals = new(StringComparer.Ordinal);

    public override void Visit(ScriptNode? node)
    {
        switch (node)
        {
            case null: return;
            case ScriptImportStatement or ScriptWithStatement:
                throw Refuse(node, TemplateCodes.Allowlist, "import and with are not available.");
            case ScriptUnaryExpression { Operator: ScriptUnaryOperator.FunctionAlias }:
                throw Refuse(node, TemplateCodes.Allowlist, "Function aliases are not available.");
            case ScriptAssignExpression assign:
                Declare(assign.Target);
                Visit(assign.Value);
                return;
            case ScriptForStatement loop:
                Declare(loop.Variable);
                break;
            case ScriptCaptureStatement capture:
                Declare(capture.Target);
                break;
            case ScriptFunction function:
                Declare(function.NameOrDoToken);
                break;
            case ScriptFunctionCall call:
                CheckCall(call);
                return;
            case ScriptMemberExpression { Target: ScriptVariableGlobal root } member when IsNamespace(root.Name):
                throw Refuse(member, TemplateCodes.Allowlist, "Builtin and helper functions are callable only, never values.");
            case ScriptVariableGlobal variable when IsNamespace(variable.Name) || _calls.Contains(variable.Name):
                throw Refuse(variable, TemplateCodes.Allowlist, "Namespaces and helper functions cannot be aliased.");
            case ScriptVariableGlobal variable when !_roots.Contains(variable.Name) && !_locals.Contains(variable.Name):
                throw Refuse(variable, TemplateCodes.Allowlist, "Unknown global: " + variable.Name);
            case ScriptLiteral { Value: string text } when TemplatePreScan.HasPrivateUse(text):
                throw Refuse(node, TemplateCodes.TemplatePrivateUse, "Private-use code point in literal.");
        }

        base.Visit(node);
    }

    static bool IsNamespace(string name) => LockedBuiltins.Members.ContainsKey(name) || _helpers.ContainsKey(name);

    static TemplateRefusal Refuse(ScriptNode node, string code, string reason) => TemplateRefusal.At(code, reason, node.Span);

    void Declare(ScriptNode? target)
    {
        if (target is not ScriptVariable variable || _roots.Contains(variable.Name) || IsNamespace(variable.Name) || _calls.Contains(variable.Name))
        {
            throw Refuse(target!, TemplateCodes.Mutation, "Only template-local variables can be assigned.");
        }

        _locals.Add(variable.Name);
    }

    void CheckCall(ScriptFunctionCall call)
    {
        if (call.Target is ScriptMemberExpression { Target: ScriptVariableGlobal root, Member: ScriptVariable member } && IsNamespace(root.Name))
        {
            var members = LockedBuiltins.Members.TryGetValue(root.Name, out var builtin) ? builtin : _helpers[root.Name];
            if (!members.Contains(member.Name, StringComparer.Ordinal)) throw Refuse(call, TemplateCodes.Allowlist, "Function is not in the profile allowlist.");
        }
        else if (call.Target is ScriptVariableGlobal global && _calls.Contains(global.Name))
        {
            if (global.Name == "render" && (call.Arguments.Count is < 1 or > 2 || call.Arguments[0] is not ScriptLiteral { Value: string name } || !kit.Manifest.Partials.Any(partial => partial.Name == name)))
            {
                throw Refuse(call, TemplateCodes.Partial, "render requires a literal declared partial name and optional argument.");
            }

            if (global.Name == "section" && (call.Arguments.Count != 2 || call.Arguments[0] is not ScriptLiteral { Value: string } || call.Arguments[1] is not ScriptLiteral { Value: string }))
            {
                throw Refuse(call, TemplateCodes.Helper, "section requires literal section and partial names.");
            }
        }
        else
        {
            Visit(call.Target);
        }

        foreach (var argument in call.Arguments) Visit(argument);
    }
}
