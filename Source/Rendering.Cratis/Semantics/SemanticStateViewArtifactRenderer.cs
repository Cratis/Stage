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
            located.Slice.Queries.Any(_ => SemanticTypeSystem.DeclarationNeedsCommon(_.Argument.Type)))
        {
            builder.Using($"{context.RootNamespace}.Common");
        }

        foreach (var projection in located.Slice.Projections)
        {
            var eventIds = projection.Scope is { } scope
                ? ScopeEvents(scope)
                : projection.Transitions.Select(_ => _.EventContract);
            foreach (var eventId in eventIds)
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
            var projection = located.Slice.Projections.Single(_ => _.ReadModel == readModel.Id);
            var transition = projection.Scope is null ? projection.Transitions.Single() : null;
            var @event = transition is null ? null : context.Events[transition.EventContract];
            var queries = located.Slice.Queries.Where(_ => _.ReadModel == readModel.Id).ToArray();
            if (@event is not null)
            {
                builder.Attribute($"FromEvent<{Identifiers.ToPascalCase(@event.Name)}>");
            }

            builder.Attribute("ReadModel")
                .OpenBlock($"public record {Identifiers.ToPascalCase(readModel.Name)}({(transition is null ? ScopedParameters(readModel, types, queries) : Parameters(readModel, transition, @event!, types, queries.FirstOrDefault()))})");
            foreach (var query in queries)
            {
                RenderQuery(builder, query, readModel, types);
            }

            builder.EndBlock();
            if (projection.Scope is { } scope)
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

    static IEnumerable<SemanticId> ScopeEvents(SemanticProjectionScope scope) =>
        scope.From.Select(_ => _.EventContract)
            .Concat(scope.Joins.Select(_ => _.EventContract))
            .Concat(scope.Removals.Select(_ => _.EventContract))
            .Concat(scope.JoinRemovals.Select(_ => _.EventContract))
            .Concat(scope.Children.SelectMany(_ => ScopeEvents(_.Scope)))
            .Concat(scope.Nested.SelectMany(_ => ScopeEvents(_.Scope))).Distinct();

    static string ScopedParameters(SemanticReadModel readModel, SemanticTypeSystem types, IReadOnlyList<SemanticKeyedQuery> queries) =>
        string.Join(", ", readModel.Properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal).Select(property =>
            $"{(property.IsIdentifier || queries.Any(_ => _.KeyProperty == property.Id) ? "[Key] " : string.Empty)}{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));

    static string Parameters(
        SemanticReadModel readModel,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticTypeSystem types,
        SemanticKeyedQuery? keyedQuery) =>
        string.Join(", ", readModel.Properties.OrderBy(property => property.Id.ToString(), StringComparer.Ordinal).Select(property =>
        {
            var mapping = transition.Mappings.Single(_ => _.TargetProperty == property.Id);
            var source = (SemanticResolvedExpression)mapping.Source;
            var eventProperty = @event.Properties.Single(_ => _.Id == source.Target);
            var targetName = Identifiers.ToPascalCase(property.Name);
            var sourceName = Identifiers.ToPascalCase(eventProperty.Name);
            var attribute = string.Equals(targetName, sourceName, StringComparison.Ordinal)
                ? string.Empty
                : $"[SetFrom<{Identifiers.ToPascalCase(@event.Name)}>(nameof({Identifiers.ToPascalCase(@event.Name)}.{sourceName}))] ";
            var key = keyedQuery?.KeyProperty == property.Id ? "[Key] " : string.Empty;
            return $"{key}{attribute}{types.Type(property.Type)} {targetName}";
        }));

    static void RenderQuery(
        CSharpCodeBuilder builder,
        SemanticKeyedQuery query,
        SemanticReadModel readModel,
        SemanticTypeSystem types)
    {
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var argumentName = Identifiers.EscapeKeyword(Identifiers.ToCamelCase(query.Argument.Name));
        builder.BlankLine()
            .Attribute(SemanticAuthorizationAttributes.For(query))
            .ExpressionMember(
                $"public static async Task<{readModelName}?> {Identifiers.ToPascalCase(query.Name)}(IReadModels readModels, {types.Type(query.Argument.Type)} {argumentName})",
                $"await readModels.GetInstanceById<{readModelName}>((EventSourceId){argumentName})");
    }
}
