// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Stage.Rendering.Kits;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

public sealed partial class TemplatePlanSession
{
    /// <summary>Renders a declared output path with the same locked helpers and plan budget.</summary>
    /// <param name="outputId">Declared output identifier.</param>
    /// <param name="scope">JSON scope with model and optional item.</param>
    /// <returns>A path string or safety refusal.</returns>
    public TemplateResult<string> RenderPath(string outputId, JsonElement scope) => Run(() =>
    {
        FindOutput(outputId);
        PushScope(scope);
        var output = new SegmentOutput(_meter);
        _context.PushOutput(output);
        _context.AllowStructuredOutput = false;
        try
        {
            _prepared.Expressions[outputId + ":path"].Render(_context);
            return string.Concat(output.Finish().Cast<TemplateTextSegment>().Select(segment => segment.Text));
        }
        finally
        {
            _context.AllowStructuredOutput = true;
            _context.PopOutput();
            _context.PopGlobal();
        }
    });

    /// <summary>Evaluates an output condition, or true if the output has no condition.</summary>
    /// <param name="outputId">Declared output identifier.</param>
    /// <param name="scope">JSON scope.</param>
    /// <returns>A Boolean or safety refusal.</returns>
    public TemplateResult<bool> EvaluateWhen(string outputId, JsonElement scope) => Run(() =>
    {
        var output = FindOutput(outputId);
        return InScope(scope, () => output.When is null || (_prepared.Expressions[outputId + ":when"].Evaluate(_context) is bool condition ? condition : throw JsonScriptValues.Invalid("when must evaluate to a Boolean.")));
    });

    /// <summary>Evaluates fan-out, or one cloned scope if no fan-out is declared.</summary>
    /// <param name="outputId">Declared output identifier.</param>
    /// <param name="scope">JSON scope.</param>
    /// <returns>Immutable cloned JSON values or safety refusal.</returns>
    public TemplateResult<ImmutableArray<JsonElement>> EvaluateForEach(string outputId, JsonElement scope) => Run(() =>
    {
        var output = FindOutput(outputId);
        return InScope<ImmutableArray<JsonElement>>(scope, () =>
        {
            if (output.ForEach is null)
            {
                _meter.FanOut(1);
                return [scope.Clone()];
            }

            if (_prepared.Expressions[outputId + ":forEach"].Evaluate(_context) is not ScriptArray values) throw JsonScriptValues.Invalid("forEach must evaluate to an array.");
            _meter.FanOut(values.Count);
            return [.. values.Select(value => JsonSerializer.SerializeToElement(JsonValue(value, 0)))];
        });
    });

    object? JsonValue(object? value, int depth)
    {
        _meter.Step();
        if (depth > 64) throw JsonScriptValues.Invalid("Evaluated JSON depth exceeds 64.");
        return value switch
        {
            null or string or bool or int or long or decimal => value,
            JsonScriptValues.JsonObject data => data.Source.Clone(),
            JsonScriptValues.JsonArray array => array.Source.Clone(),
            ScriptArray array => array.Select(item => JsonValue(item, depth + 1)).ToArray(),
            ScriptObject data => data.Keys.ToDictionary(key => key, key => JsonValue(data[key], depth + 1), StringComparer.Ordinal),
            _ => throw JsonScriptValues.Invalid("forEach values must be JSON, not symbols or functions.")
        };
    }

    KitOutput FindOutput(string outputId) => _prepared.Kit.Manifest.Outputs.FirstOrDefault(output => output.Id == outputId) ?? throw JsonScriptValues.Invalid("Unknown output identifier: " + outputId);

    T InScope<T>(JsonElement scope, Func<T> action)
    {
        PushScope(scope);
        try { return action(); }
        finally { _context.PopGlobal(); }
    }
}
