// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the admitted state-view ESM capability as a Cratis model-bound read model.
/// </summary>
internal static class SemanticStateViewArtifactRenderer
{
    /// <summary>
    /// Renders one state-view slice.
    /// </summary>
    /// <param name="located">The located semantic slice.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated slice source.</returns>
    public static RenderedFile Render(LocatedSemanticSlice located, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels");
        if (located.Slice.ReadModels.SelectMany(_ => _.Properties).Any(_ => SemanticTypeSystem.DeclarationNeedsCommon(_.Type)) ||
            located.Slice.Queries.Any(_ => _.Argument is not null && SemanticTypeSystem.DeclarationNeedsCommon(_.Argument.Type)))
        {
            builder.Using($"{context.RootNamespace}.Common");
        }

        foreach (var projection in located.Slice.Projections)
        {
            foreach (var eventId in ProjectionReferencedEventNamesAreUnique.Contracts(projection))
            {
                var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(eventId).Path);
                if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
                {
                    builder.Using(eventNamespace);
                }
            }
        }

        if (located.Slice.Queries.Length > 0 || located.Slice.Projections.Any(_ => _.Scope is not null))
        {
            builder.Using("Cratis.Chronicle.Keys");
        }

        var firstModel = true;
        foreach (var readModel in located.Slice.ReadModels)
        {
            if (!firstModel)
            {
                builder.BlankLine();
            }

            firstModel = false;
            var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == readModel.Id);
            var transition = projection?.Scope is null && projection is not null ? projection.Transitions.Single() : null;
            var @event = transition is null ? null : context.Events[transition.EventContract];
            var queries = located.Slice.Queries.Where(_ => _.ReadModel == readModel.Id).ToArray();
            context.Docs(readModel.Id).Render(builder);
            if (@event is not null)
            {
                builder.Attribute($"global::Cratis.Chronicle.Projections.ModelBound.FromEventAttribute<{types.EventType(@event)}>");
            }

            builder.Attribute("global::Cratis.Arc.Queries.ModelBound.ReadModelAttribute")
                .OpenBlock($"public record {Identifiers.ToPascalCase(readModel.Name)}({(transition is null ? ScopedParameters(readModel, types, queries, context.Reducers.Any(reducer => reducer.ReadModel == readModel.Id)) : Parameters(readModel, transition, @event!, types, queries.FirstOrDefault(query => query.Cardinality == SemanticQueryCardinality.ZeroOrOne)))})");
            foreach (var query in queries)
            {
                RenderQuery(builder, query, readModel, types, context);
            }

            builder.EndBlock();
            if (projection?.Scope is { } scope)
            {
                builder.BlankLine().Raw(SemanticScopedProjectionRenderer.Render(projection, readModel, scope, context));
            }
        }

        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString())
        {
            Sources = [located.Slice.Id, .. located.Slice.ReadModels.Select(_ => _.Id),
                .. located.Slice.Projections.Select(_ => _.Id), .. located.Slice.Queries.Select(_ => _.Id)]
        };
    }

    internal static IOrderedEnumerable<SemanticProperty> OrderedProperties(IEnumerable<SemanticProperty> properties) =>
        properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal);

    static string ScopedParameters(SemanticReadModel readModel, SemanticTypeSystem types, IReadOnlyList<SemanticKeyedQuery> queries, bool reducerInput) =>
        string.Join(", ", OrderedProperties(readModel.Properties).Select(property =>
            $"{((property.IsIdentifier || queries.Any(_ => _.Cardinality == SemanticQueryCardinality.ZeroOrOne && _.KeyProperty == property.Id)) && !types.IsEventSourceIdentifier(property.Type) ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty)}{types.Type(property.Type, reducerInput)} {Identifiers.ToPascalCase(property.Name)}"));

    static string Parameters(
        SemanticReadModel readModel,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticTypeSystem types,
        SemanticKeyedQuery? keyedQuery) =>
        string.Join(", ", OrderedProperties(readModel.Properties).Select(property =>
        {
            var mapping = transition.Mappings.Single(_ => _.TargetProperty == property.Id);
            var source = (SemanticResolvedExpression)mapping.Source;
            var eventProperty = @event.Properties.Single(_ => _.Id == source.Target);
            var targetName = Identifiers.ToPascalCase(property.Name);
            var sourceName = Identifiers.ToPascalCase(eventProperty.Name);
            var attribute = string.Equals(targetName, sourceName, StringComparison.Ordinal)
                ? string.Empty
                : $"[global::Cratis.Chronicle.Projections.ModelBound.SetFromAttribute<{types.EventType(@event)}>(nameof({types.EventType(@event)}.{sourceName}))] ";
            var key = keyedQuery?.KeyProperty == property.Id && !types.IsEventSourceIdentifier(property.Type) ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty;
            return $"{key}{attribute}{types.Type(property.Type)} {targetName}";
        }));

    static void RenderQuery(
        CSharpCodeBuilder builder,
        SemanticKeyedQuery query,
        SemanticReadModel readModel,
        SemanticTypeSystem types,
        SemanticApplicationContext context)
    {
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var methodName = Identifiers.ToPascalCase(query.Name);
        var documentation = context.Docs(query.Id).Render(builder.BlankLine())
            .Attribute(SemanticAuthorizationAttributes.For(query));
        if (query.Argument is null)
        {
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels)",
                $"await readModels.GetInstances<{readModelName}>()");
            return;
        }

        var argumentName = Identifiers.EscapeKeyword(Identifiers.ToCamelCase(query.Argument.Name));
        var argumentType = types.Type(query.Argument.Type);
        if (query.Cardinality == SemanticQueryCardinality.Many)
        {
            var property = readModel.Properties.Single(_ => _.Id == query.KeyProperty);
            documentation.ExpressionMember(
                $"public static async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<{readModelName}>> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {argumentType} {argumentName})",
                $"global::System.Linq.Enumerable.Where(await readModels.GetInstances<{readModelName}>(), instance => global::System.Collections.Generic.EqualityComparer<{argumentType}>.Default.Equals(instance.{Identifiers.ToPascalCase(property.Name)}, {argumentName}))");
            return;
        }

        documentation.ExpressionMember(
            $"public static async global::System.Threading.Tasks.Task<{readModelName}?> {methodName}(global::Cratis.Chronicle.ReadModels.IReadModels readModels, {argumentType} {argumentName})",
            $"await readModels.GetInstanceById<{readModelName}>((global::Cratis.Chronicle.Events.EventSourceId){argumentName})");
    }
}
