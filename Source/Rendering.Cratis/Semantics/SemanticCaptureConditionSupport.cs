// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Names the capture guards an inbox reactor cannot evaluate exactly as the reference capture evaluator does.
/// </summary>
internal static class SemanticCaptureConditionSupport
{
    /// <summary>Finds every guard that the generated capture reactor cannot render faithfully.</summary>
    /// <param name="capture">The capture.</param>
    /// <param name="context">The application context.</param>
    /// <returns>A reason per refused guard; empty when every guard renders.</returns>
    internal static IEnumerable<string> Refusals(SemanticCapture capture, SemanticApplicationContext context)
    {
        var sources = capture.EventsSource?.Events.Select(id => context.Events[id]).ToArray() ?? [];
        foreach (var when in capture.Appends.Select(append => append.When).OfType<SemanticCaptureCondition>())
        {
            switch (when.Kind)
            {
                case SemanticCaptureConditionKind.Added or SemanticCaptureConditionKind.Removed:
                    break;
                case SemanticCaptureConditionKind.AnyChanged or SemanticCaptureConditionKind.AllChanged or SemanticCaptureConditionKind.Transition:
                    foreach (var reason in when.Fields.SelectMany(field => FieldRefusals(when, field, sources, context)))
                    {
                        yield return reason;
                    }
                    break;
                case SemanticCaptureConditionKind.Expression:
                    yield return $"expression guard '{when.Expression}' requires the reference capture expression evaluator, which is not rendered";
                    break;
                default:
                    yield return $"guard '{when.Kind}' is not known to the renderer";
                    break;
            }
        }
    }

    static IEnumerable<string> FieldRefusals(SemanticCaptureCondition when, string field, IEnumerable<SemanticEventContract> sources, SemanticApplicationContext context)
    {
        if (field.Contains('.', StringComparison.Ordinal))
        {
            yield return $"'{when.Kind}' guard path '{field}' requires traversal the reference evaluator does not support";
            yield break;
        }
        foreach (var property in sources.SelectMany(source => source.Properties).Where(property => property.Name == field))
        {
            if (property.Type.IsCollection || property.Type.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                yield return $"'{when.Kind}' guard field '{field}' is a record or list value the reference evaluator cannot use as a scalar";
                yield break;
            }
            if (when.Kind == SemanticCaptureConditionKind.Transition && !TransitionComparable(property.Type, context))
            {
                yield return $"transition guard field '{field}' is not text, a number or a boolean, so its rendered text would differ from the modeled value";
                yield break;
            }
        }
    }

    static bool TransitionComparable(SemanticTypeReference type, SemanticApplicationContext context)
    {
        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when context.Concepts.TryGetValue(type.Target, out var concept) && concept.Values.IsEmpty => concept.Primitive,
            _ => SemanticPrimitiveType.Unknown
        };
        return primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;
    }
}
