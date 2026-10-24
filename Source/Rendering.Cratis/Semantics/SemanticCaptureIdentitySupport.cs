// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static class SemanticCaptureIdentitySupport
{
    internal const string Code = "STAGE-ESM-052";

    internal static string? Check(SemanticCapture capture, ExecutableSemanticModel model, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
    {
        if (!UsesTypedIdentity(model.SemanticVersion) || capture.EventsSource is not { } source) return null;
        var keys = source.Events.SelectMany(id => events[id].Properties.Where(property => property.Name == capture.Key)).ToArray();
        if (!HasEverySourceKey(keys.Length, source.Events.Length) || keys.Any(key => !RequiredScalar(key.Type)))
        {
            return $"Capture '{capture.Name}' key '{capture.Key}' must be one required scalar property on every source event.";
        }
        var type = keys[0].Type;
        if (keys.Any(key => key.Type != type) || !Convertible(type, model.Application))
        {
            return $"Capture '{capture.Name}' key '{capture.Key}' must have one consistent Text, Uuid or WholeNumber identity type (or a non-enumerated concept over it).";
        }
        return CheckAppendIdentities(capture, type);
    }

    static bool HasEverySourceKey(int keyCount, int sourceCount) => keyCount == sourceCount && keyCount > 0;

    static bool RequiredScalar(SemanticTypeReference type) => !type.IsOptional && !type.IsCollection;

    static string? CheckAppendIdentities(SemanticCapture capture, SemanticTypeReference type)
    {
        var appends = capture.Appends.Concat((capture.Children.IsDefault ? [] : capture.Children).SelectMany(children => children.Appends));
        return appends.Any(append => append.EventSourceType != type)
            ? $"Capture '{capture.Name}' key '{capture.Key}' does not match every append's declared event-source identity type; Stage will not coerce the key to text."
            : null;
    }

    internal static bool UsesTypedIdentity(SemanticVersion version) => version == SemanticVersion.V32 || version == SemanticVersion.V33;

    static bool Convertible(SemanticTypeReference type, SemanticApplication application)
    {
        var primitive = type.Primitive;
        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            var concept = application.Concepts.Single(concept => concept.Id == type.Target);
            if (!concept.Values.IsDefaultOrEmpty) return false;
            primitive = concept.Primitive;
        }
        return type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept &&
            primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.WholeNumber;
    }
}
