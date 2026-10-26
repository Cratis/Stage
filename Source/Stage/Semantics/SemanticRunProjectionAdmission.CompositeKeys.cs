// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

internal static partial class SemanticRunProjectionAdmission
{
    internal static string? CheckCompositeKey(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticReadModel readModel, SemanticProjectionScope scope) =>
        CompositeKeyRejection(plan, specification, readModel, scope);

    static SemanticUnsupportedCapability? CompositeSeedRejection(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        var seed = specification.GivenReadModels.FirstOrDefault(seed =>
            plan.ReadModels.TryGetValue(seed.ReadModel, out var model) && model.Properties.Count(property => property.IsIdentifier) > 1);

        return seed is null ? null : new(StageExecutionCapability.GivenReadModel, seed.ReadModel.ToString(), "Given readmodel seeds with composite keys are not supported by the per-run projection scenario.");
    }

    static bool QueryKeyMatches(SemanticReadModel model, SemanticKeyedQuery query)
    {
        var identifiers = model.Properties.Where(property => property.IsIdentifier).Select(property => property.Id).ToArray();

        return identifiers.Length > 1
            ? query.KeyParts.Length == identifiers.Length && query.KeyParts.Select(part => part.KeyProperty).ToHashSet().SetEquals(identifiers)
            : identifiers.Length == 1 && query.KeyProperty == identifiers[0];
    }

    static string? CompositeProjectionRejection(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticProjection projection, HashSet<SemanticId> reachable)
    {
        var readModel = plan.ReadModels[projection.ReadModel];
        var scope = projection.Scope!;
        if (CompositeKeyRejection(plan, specification, readModel, scope) is { } rejection)
        {
            return rejection;
        }
        if (ProductRejection(plan, scope, readModel, reachable) is { } product)
        {
            return $"A projection adds or subtracts a product of two event values (ESM v29) that cannot be carried on the event: {product}.";
        }

        return MappingFreeCompositeFrom(scope, readModel)
            ? "A mapping-free from block on a read model without required properties does not materialize a Chronicle read model."
            : null;
    }

    static bool MappingFreeCompositeFrom(SemanticProjectionScope scope, SemanticReadModel readModel) =>
        readModel.Properties.All(property => property.IsIdentifier || property.Type.IsOptional) && scope.From.Any(from => from.Mappings.IsEmpty);

    static string? CompositeKeyRejection(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticReadModel readModel, SemanticProjectionScope scope)
    {
        if (!CompositeShapeSupported(plan, readModel))
        {
            return "Composite key parts must be required, single, non-date primitives or concepts and must not collide with the runtime Id property.";
        }
        if (specification.GivenReadModels.Any(given => given.ReadModel == readModel.Id))
        {
            return "Given readmodel seeds with composite keys are not supported by the per-run projection scenario.";
        }
        if (!CompositeRootScope(scope))
        {
            return "Composite-keyed projections run only root from and remove-with blocks.";
        }
        if (!CompositeScopeKeysMatch(plan, readModel, scope))
        {
            return "Every from and remove-with block needs all composite key parts exactly once from compatible required event properties, with no parent key.";
        }
        if (scope.From.Any(from => from.Mappings.Any(mapping => !MappingSupported(mapping))))
        {
            return "This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.";
        }

        return SemanticScopedProjectionSupport.Rejection(scope, new SemanticApplicationContext(plan), readModel.Properties);
    }

    static bool CompositeShapeSupported(SemanticExecutionPlan plan, SemanticReadModel model) =>
        !model.Properties.Any(property => string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase)) &&
        model.Properties.Where(property => property.IsIdentifier).All(part => RequiredCompositePart(plan, part.Type));

    static bool RequiredCompositePart(SemanticExecutionPlan plan, SemanticTypeReference type) =>
        !type.IsOptional && !type.IsCollection && CompositePrimitiveSupported(plan, type);

    static bool CompositePrimitiveSupported(SemanticExecutionPlan plan, SemanticTypeReference type) =>
        IsScalar(type) && Primitive(plan, type) != SemanticPrimitiveType.DateTime;

    static bool CompositeRootScope(SemanticProjectionScope scope) =>
        !(scope.From.IsEmpty && scope.Removals.IsEmpty) && OnlyCompositeRootOperations(scope);

    static bool OnlyCompositeRootOperations(SemanticProjectionScope scope) =>
        NoCompositeJoinsOrEvery(scope) && scope.Children.IsEmpty && scope.Nested.IsEmpty;

    static bool NoCompositeJoinsOrEvery(SemanticProjectionScope scope) =>
        scope.Joins.IsEmpty && scope.JoinRemovals.IsEmpty && scope.Every is null;

    static bool CompositeScopeKeysMatch(SemanticExecutionPlan plan, SemanticReadModel model, SemanticProjectionScope scope) =>
        scope.From.All(from => CompositeFromMatches(plan, model, from)) &&
        scope.Removals.All(removal => removal.ParentKey is null && CompositeKeyMatches(plan, model, removal.Key, removal.EventContract));

    static bool CompositeFromMatches(SemanticExecutionPlan plan, SemanticReadModel model, SemanticProjectionFrom from) =>
        from.ParentKey is null && CompositeKeyMatches(plan, model, from.Key, from.EventContract) &&
        KeyMappingsMatch(from, [.. model.Properties.Where(property => property.IsIdentifier)]);

    static bool CompositeKeyMatches(SemanticExecutionPlan plan, SemanticReadModel model, SemanticProjectionKey key, SemanticId eventContract)
    {
        var identifiers = model.Properties.Where(property => property.IsIdentifier).ToArray();

        return key is SemanticProjectionPartsKey parts && CompleteCompositeParts(parts, identifiers.Length) &&
            parts.Parts.All(part => CompositePartMatches(plan, identifiers, part, plan.Events[eventContract]));
    }

    static bool CompleteCompositeParts(SemanticProjectionPartsKey parts, int count) =>
        parts.Parts.Length == count && parts.Parts.Select(part => part.Property).Distinct().Count() == count;

    static bool CompositePartMatches(SemanticExecutionPlan plan, SemanticProperty[] identifiers, SemanticProjectionKeyPart part, SemanticEventContract @event) =>
        identifiers.SingleOrDefault(identifier => identifier.Id == part.Property) is { } identifier &&
        CompositePartSource(plan, @event, part.Value) is { } property && Primitive(plan, property.Type) == Primitive(plan, identifier.Type);

    static SemanticProperty? CompositePartSource(SemanticExecutionPlan plan, SemanticEventContract @event, SemanticProjectionValue value) =>
        value is SemanticProjectionEventProperty source && !source.Path.IsEmpty ? RequiredKeyProperty(plan, @event, source) : null;

    static bool KeyMappingsMatch(SemanticProjectionFrom from, SemanticProperty[] identifiers) =>
        from.Mappings.Where(mapping => identifiers.Any(part => mapping.Target.Contains(part.Id))).All(mapping => KeyMappingMatches(from.Key, mapping));

    static bool KeyMappingMatches(SemanticProjectionKey key, SemanticProjectionMapping mapping) =>
        mapping.Operation == SemanticProjectionOperation.Set && mapping.Target.Length == 1 && KeyMappingSourceMatches(key, mapping);

    static bool KeyMappingSourceMatches(SemanticProjectionKey key, SemanticProjectionMapping mapping) =>
        mapping.Source is SemanticProjectionEventProperty source && key is SemanticProjectionPartsKey parts &&
        parts.Parts.Single(part => part.Property == mapping.Target[0]).Value is SemanticProjectionEventProperty keySource &&
        source.Path.SequenceEqual(keySource.Path);

    static SemanticProperty? RequiredKeyProperty(SemanticExecutionPlan plan, SemanticEventContract @event, SemanticProjectionEventProperty source)
    {
        var properties = @event.Properties;
        SemanticProperty? property = null;
        foreach (var id in source.Path)
        {
            property = properties.SingleOrDefault(candidate => candidate.Id == id);
            if (property?.Type.IsOptional != false || property.Type.IsCollection) return null;
            if (property.Type.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                properties = plan.Model.Application.Types.Single(type => type.Id == property.Type.Target).Properties;
            }
        }

        return property;
    }
}
