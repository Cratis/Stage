// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class TemplateLint(LoadedKit kit, Dictionary<string, string>? sections = null) : ScriptVisitor
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
    readonly TemplatePartialLint _partials = new(kit, sections);

    public override void Visit(ScriptNode? node)
    {
        if (node is null || VisitExpression(node)) return;
        CheckDeclaration(node);
        CheckForbiddenNode(node);
        if (node is ScriptVariableGlobal variable) CheckGlobal(variable);
        base.Visit(node);
    }

    static bool IsNamespace(string name) => LockedBuiltins.Members.ContainsKey(name) || _helpers.ContainsKey(name);

    static bool IsReserved(string name) => _roots.Contains(name) || IsNamespace(name) || _calls.Contains(name);

    static TemplateRefusal Refuse(ScriptNode node, string code, string reason) => TemplateRefusal.At(code, reason, node.Span);

    bool VisitExpression(ScriptNode node)
    {
        switch (node)
        {
            case ScriptAssignExpression assign:
                Declare(assign.Target);
                Visit(assign.Value);
                return true;
            case ScriptFunctionCall call:
                CheckCall(call);
                return true;
            case ScriptIndexerExpression { Target: ScriptVariableGlobal { Name: "lit" }, Index: ScriptLiteral { Value: "null" } }:
                return true;
            case ScriptMemberExpression member:
                if (member.Target is ScriptVariableGlobal root && IsNamespace(root.Name)) throw Refuse(member, TemplateCodes.Allowlist, "Builtin and helper functions are callable only, never values.");
                Visit(member.Target);
                return true;
            default: return false;
        }
    }

    static void CheckForbiddenNode(ScriptNode node)
    {
        switch (node)
        {
            case ScriptImportStatement or ScriptWithStatement:
                throw Refuse(node, TemplateCodes.Allowlist, "import and with are not available.");
            case ScriptUnaryExpression { Operator: ScriptUnaryOperator.FunctionAlias }:
                throw Refuse(node, TemplateCodes.Allowlist, "Function aliases are not available.");
            case ScriptLiteral { Value: string text } when TemplatePreScan.HasPrivateUse(text):
                throw Refuse(node, TemplateCodes.TemplatePrivateUse, "Private-use code point in literal.");
        }
    }

    void CheckDeclaration(ScriptNode node)
    {
        switch (node)
        {
            case ScriptForStatement loop: Declare(loop.Variable); break;
            case ScriptCaptureStatement capture: Declare(capture.Target); break;
            case ScriptParameter parameter: Declare(parameter.Name); break;
            case ScriptFunction function: Declare(function.NameOrDoToken); break;
        }
    }

    void CheckGlobal(ScriptVariableGlobal variable)
    {
        if (IsNamespace(variable.Name) || _calls.Contains(variable.Name)) throw Refuse(variable, TemplateCodes.Allowlist, "Namespaces and helper functions cannot be aliased.");
        if (!_roots.Contains(variable.Name) && !_locals.Contains(variable.Name)) throw Refuse(variable, TemplateCodes.Allowlist, "Unknown global: " + variable.Name);
    }

    void Declare(ScriptNode? target)
    {
        if (target is not ScriptVariable variable) throw Refuse(target!, TemplateCodes.Mutation, "Only template-local variables can be assigned.");
        if (IsReserved(variable.Name)) throw Refuse(target, TemplateCodes.Mutation, "Only template-local variables can be assigned.");
        _locals.Add(variable.Name);
    }

    void CheckCall(ScriptFunctionCall call)
    {
        if (call.Target is ScriptMemberExpression { Target: ScriptVariableGlobal root } member && IsNamespace(root.Name)) CheckNamespaceCall(call, member);
        else if (call.Target is ScriptVariableGlobal global && _calls.Contains(global.Name)) CheckProfileCall(call, global);
        else Visit(call.Target);
        foreach (var argument in call.Arguments) Visit(argument);
    }

    void CheckNamespaceCall(ScriptFunctionCall call, ScriptMemberExpression member)
    {
        var root = ((ScriptVariableGlobal)member.Target!).Name;
        var members = LockedBuiltins.Members.TryGetValue(root, out var builtin) ? builtin : _helpers[root];
        if (!members.Contains(member.Member!.Name, StringComparer.Ordinal)) throw Refuse(call, TemplateCodes.Allowlist, "Function is not in the profile allowlist.");
    }

    void CheckProfileCall(ScriptFunctionCall call, ScriptVariableGlobal name)
    {
        if (name.Name == "render") _partials.Render(call);
        if (name.Name == "section") _partials.Section(call);
    }
}
