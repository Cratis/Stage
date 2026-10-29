// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal enum RenderTypeKind
{
    Primitive,
    Concept,
    Composite
}

internal sealed record RenderTypeRef(RenderTypeKind Kind, SemanticPrimitiveType? Primitive, SemanticId? Target, bool IsCollection, bool IsOptional);

internal sealed class RenderTypeResolver(SemanticApplication application)
{
    internal static bool IsPrimitive(SemanticPrimitiveType primitive) => primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

    internal RenderTypeRef Resolve(SemanticTypeReference type) => Resolve(type, []);

    internal RenderTypeRef Property(SemanticId id)
    {
        var slices = RenderScopeSelection.Locate(application).Select(value => value.Slice).ToArray();
        var properties = application.Types.SelectMany(value => value.Properties)
            .Concat(application.Triggers.SelectMany(value => value.Properties))
            .Concat(slices.SelectMany(value => value.Commands).SelectMany(value => value.Properties))
            .Concat(slices.SelectMany(value => value.Events).SelectMany(value => value.Properties))
            .Concat(slices.SelectMany(value => value.ReadModels).SelectMany(value => value.Properties));
        var property = properties.FirstOrDefault(value => value.Id == id) ?? throw new InvalidArtifactRenderContract($"Missing property declaration '{id}'.");

        return Resolve(property.Type);
    }

    RenderTypeRef Resolve(SemanticTypeReference type, HashSet<SemanticId> visited) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive when IsPrimitive(type.Primitive) => new(RenderTypeKind.Primitive, type.Primitive, null, type.IsCollection, type.IsOptional),
        SemanticTypeReferenceKind.Concept => Concept(type),
        SemanticTypeReferenceKind.CompositeType => Composite(type, visited),
        _ => throw new InvalidArtifactRenderContract($"Unclassified type '{type.Kind}/{type.Primitive}' targeting '{type.Target}'.")
    };

    RenderTypeRef Concept(SemanticTypeReference type)
    {
        var concept = application.Concepts.FirstOrDefault(value => value.Id == type.Target);
        if (concept is null || !IsPrimitive(concept.Primitive))
        {
            throw new InvalidArtifactRenderContract($"Missing or unclassified concept declaration '{type.Target}'.");
        }

        return new(RenderTypeKind.Concept, concept.Primitive, concept.Id, type.IsCollection, type.IsOptional);
    }

    RenderTypeRef Composite(SemanticTypeReference type, HashSet<SemanticId> visited)
    {
        var composite = application.Types.FirstOrDefault(value => value.Id == type.Target) ?? throw new InvalidArtifactRenderContract($"Missing composite declaration '{type.Target}'.");
        if (visited.Add(composite.Id))
        {
            foreach (var property in composite.Properties)
            {
                Resolve(property.Type, visited);
            }
        }

        return new(RenderTypeKind.Composite, null, composite.Id, type.IsCollection, type.IsOptional);
    }
}
