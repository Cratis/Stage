// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class LockedContext : TemplateContext
{
    readonly PlanMeter _meter;

    internal LockedContext(TemplateBudget budget, PlanMeter meter, CancellationToken deadline) : base(LockedBuiltins.Create(), StringComparer.Ordinal)
    {
        _meter = meter;
        MemberFilter = _ => false;
        StrictVariables = true;
        ObjectRecursionLimit = 16;
        LoopLimit = (int)Math.Min(1_000_000, budget.MaxSteps);
        LimitToString = budget.MaxStringLength;
        RecursiveLimit = budget.MaxRecursion;
        OutputLimit = (int)Math.Min(budget.MaxOutputBytes, int.MaxValue);
        OnStringLimit = ScriptLimitBehavior.Throw;
        OnOutputLimit = ScriptLimitBehavior.Throw;
        CancellationToken = deadline;
        PushCulture(CultureInfo.InvariantCulture);
    }

    public override object? Evaluate(ScriptNode? scriptNode, bool aliasReturnedFunction)
    {
        if (scriptNode is not null) _meter.Step();
        var value = base.Evaluate(scriptNode, aliasReturnedFunction);
        if (value is SymbolHandle or SegmentHandle && scriptNode is not ScriptExpressionStatement && scriptNode?.Parent is not (ScriptExpressionStatement or ScriptAssignExpression)) throw SymbolHandle.NotText();
        return value;
    }

    public override TemplateContext Write(SourceSpan span, object? textAsObject)
    {
        if (textAsObject is not (SymbolHandle or SegmentHandle)) return base.Write(span, textAsObject);
        if (OutputCount != 2 || Output is not SegmentOutput segments) throw SymbolHandle.NotText();
        if (textAsObject is SymbolHandle symbol) segments.Symbol(symbol.Descriptor);
        else segments.Emit(((SegmentHandle)textAsObject).Segments);
        return this;
    }

    public override bool ToBool(SourceSpan span, object? value)
    {
        if (value is SymbolHandle or SegmentHandle) throw SymbolHandle.NotText();
        return base.ToBool(span, value);
    }

    public override string? ObjectToString(object? value, bool nested = false)
    {
        if (value is SymbolHandle or SegmentHandle) throw SymbolHandle.NotText();
        if (value is null or string or IScriptObject or bool or int or long or decimal or double or float or ScriptRange) return base.ObjectToString(value, nested);
        throw TemplateRefusal.At(TemplateCodes.HostValue, "Host values cannot be rendered.", CurrentSpan);
    }

    protected override IObjectAccessor? GetMemberAccessorImpl(object target)
    {
        if (target is SymbolHandle or SegmentHandle) throw SymbolHandle.NotText();
        if (target is IScriptObject) return base.GetMemberAccessorImpl(target);
        throw TemplateRefusal.At(TemplateCodes.HostValue, "CLR members are not accessible.", CurrentSpan);
    }
}
