// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>A single-use plan context. A refusal invalidates every output from this session.</summary>
public sealed partial class TemplatePlanSession : IDisposable
{
    readonly PreparedTemplateKit _prepared;
    readonly TemplateBudget _budget;
    readonly PlanMeter _meter;
    readonly CancellationTokenSource _deadline = new();
    readonly LockedContext _context;
    bool _disposed;

    internal TemplatePlanSession(PreparedTemplateKit prepared, TemplateBudget budget, ITemplateLanguage language, ITypeDirectory types)
    {
        _prepared = prepared;
        _budget = budget;
        _meter = new(budget);
        if (budget.MaxSteps <= 0 || budget.MaxOutputBytes <= 0 || budget.MaxArtifacts <= 0 || budget.MaxFanOut <= 0 || budget.MaxStringLength <= 0 || budget.MaxRecursion <= 0 || budget.Deadline <= TimeSpan.Zero || budget.Deadline.TotalMilliseconds > uint.MaxValue - 1)
        {
            Refusal = new(TemplateCodes.Input, "Every budget limit must be positive and the deadline must fit the timer.", string.Empty, 0, 0);
        }
        else
        {
            _deadline.CancelAfter(budget.Deadline);
        }

        _context = new(budget, _meter, _deadline.Token);
        var helpers = new ScriptObject();
        helpers.Import("render", new Func<string, object?, object?>(RenderPartial));
        _context.Section = RenderSection;
        var symbols = new ScriptObject();
        symbols.Import("decl", new Func<object?, object?, SymbolHandle>((ns, name) => SymbolHandle.Create(TemplateSymbolKind.Declaration, ns, name)));
        symbols.Import("ref", new Func<object?, object?, SymbolHandle>((ns, name) => SymbolHandle.Create(TemplateSymbolKind.Reference, ns, name)));
        symbols.Import("imports", new Func<SymbolHandle>(() => new(new(TemplateSymbolKind.Imports, null))));
        symbols.IsReadOnly = true;
        helpers.SetValue("sym", symbols, true);
        var implementations = new ScriptObject();
        implementations.Import("body", new Func<object?, AuthoredHandle>(AuthoredHandle.Create));
        implementations.IsReadOnly = true;
        helpers.SetValue("impl", implementations, true);
        LanguageHelpers.Install(helpers, language, budget.MaxStringLength);
        InstallSemantics(helpers, language, types);
        _context.PushGlobal(helpers);
    }

    /// <summary>Gets whether any operation refused this plan.</summary>
    public bool IsRefused => Refusal is not null;

    /// <summary>Gets the first refusal. Previously returned artifacts must be discarded.</summary>
    public TemplateDiagnostic? Refusal { get; private set; }

    /// <summary>Renders one declared template in a JSON scope.</summary>
    /// <param name="templatePath">Declared kit-relative template path.</param>
    /// <param name="scope">JSON object containing model and optional item.</param>
    /// <returns>Output segments, or a safety refusal and no output.</returns>
    public TemplateRenderResult Render(string templatePath, JsonElement scope)
    {
        var result = Run(() =>
        {
            _gaps.Clear();
            _meter.Artifact();
            if (!_prepared.Templates.TryGetValue(templatePath, out var template)) throw JsonScriptValues.Invalid("Template is not declared.");
            PushScope(scope);
            try
            {
                var output = new SegmentOutput(_meter);
                _context.PushOutput(output);
                try { template.Render(_context); }
                finally { _context.PopOutput(); }
                return new TemplateOutput(output.Finish(), [.. _gaps]);
            }
            finally { _context.PopGlobal(); }
        });
        return new(result.Value, result.Refusal);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _deadline.Dispose();
    }

    internal TemplateResult<T> Run<T>(Func<T> action)
    {
        if (IsRefused) return TemplateResult<T>.Refused(new TemplateDiagnostic(TemplateCodes.AlreadyRefused, "Plan was already refused; discard all its outputs.", string.Empty, 0, 0));
        try
        {
            if (_disposed) throw JsonScriptValues.Invalid("Plan is disposed.");
            _meter.CheckDeadline();
            return TemplateResult<T>.Success(action());
        }
        catch (Exception error) when (error is TemplateRefusal or TemplateLanguageRefusal or ScriptRuntimeException or OperationCanceledException or ArgumentException or OverflowException or InvalidOperationException or KeyNotFoundException)
        {
            Refusal = TemplateErrorMap.Map(error);
            return TemplateResult<T>.Refused(Refusal);
        }
    }

    void PushScope(JsonElement scope)
    {
        if (JsonScriptValues.Convert(scope, _budget.MaxStringLength) is not ScriptObject data) throw JsonScriptValues.Invalid("Scope must be a JSON object.");
        if (data.Keys.Any(key => key != "model" && key != "item")) throw JsonScriptValues.Invalid("Scope contains an unknown binding.");
        var bindings = new ScriptObject();
        foreach (var key in data.Keys) bindings.SetValue(key, data[key], true);
        _context.PushGlobal(bindings);
    }

    object? RenderSection(ScriptWrapStatement wrap)
    {
        var call = (ScriptFunctionCall)wrap.Target!;
        var name = (string)((ScriptLiteral)call.Arguments[0]).Value!;
        var body = _prepared.Overrides.TryGetValue((wrap.Span.FileName, name), out var replacement) ? replacement.Page!.Body : wrap.Body;
        return _context.Evaluate(body, false);
    }

    object? RenderPartial(string name, object? argument = null)
    {
        var partial = _prepared.Kit.Manifest.Partials.FirstOrDefault(partial => partial.Name == name)
            ?? throw TemplateRefusal.At(TemplateCodes.Partial, "Partial is not declared.", _context.CurrentSpan);
        _context.EnterRecursive(_context.CurrentNode);
        var scope = new ScriptObject();
        scope.SetValue("item", argument, true);
        _context.PushGlobal(scope);
        try { _context.Evaluate(_prepared.Templates[partial.Template].Page, false); }
        finally
        {
            _context.PopGlobal();
            _context.ExitRecursive(_context.CurrentNode);
        }

        return null;
    }
}
