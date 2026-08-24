// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

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
        var readModel = located.Slice.ReadModels.Single();
        var projection = located.Slice.Projections.Single();
        var transition = projection.Transitions.Single();
        var @event = context.Events[transition.EventContract];
        var ownNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event.Id).Path);
        var builder = new CSharpCodeBuilder()
            .Namespace(ownNamespace)
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Queries.ModelBound")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.Projections.ModelBound")
            .Using("Cratis.Chronicle.ReadModels")
            .Using($"{context.RootNamespace}.Common");
        if (!string.Equals(ownNamespace, eventNamespace, StringComparison.Ordinal))
        {
            builder.Using(eventNamespace);
        }

        builder.Attribute($"FromEvent<{Identifiers.ToPascalCase(@event.Name)}>")
            .Attribute("ReadModel")
            .OpenBlock($"public record {Identifiers.ToPascalCase(readModel.Name)}({Parameters(readModel, transition, @event, types)})");
        if (located.Slice.Queries.SingleOrDefault() is { } query)
        {
            RenderQuery(builder, query, readModel, types);
        }

        builder.EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), SliceNaming.FileName(located.Slice.Name)]);
        return new(path, builder.ToString());
    }

    static string Parameters(
        SemanticReadModel readModel,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticTypeSystem types) =>
        string.Join(", ", readModel.Properties.Select(property =>
        {
            var mapping = transition.Mappings.Single(_ => _.TargetProperty == property.Id);
            var source = (SemanticResolvedExpression)mapping.Source;
            var eventProperty = @event.Properties.Single(_ => _.Id == source.Target);
            var targetName = Identifiers.ToPascalCase(property.Name);
            var sourceName = Identifiers.ToPascalCase(eventProperty.Name);
            var attribute = string.Equals(targetName, sourceName, StringComparison.Ordinal)
                ? string.Empty
                : $"[SetFrom<{Identifiers.ToPascalCase(@event.Name)}>(nameof({Identifiers.ToPascalCase(@event.Name)}.{sourceName}))] ";
            return $"{attribute}{types.Type(property.Type)} {targetName}";
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
            .Attribute("AllowAnonymous")
            .ExpressionMember(
                $"public static async Task<{readModelName}?> {Identifiers.ToPascalCase(query.Name)}(IReadModels readModels, {types.Type(query.Argument.Type)} {argumentName})",
                $"await readModels.GetInstanceById<{readModelName}>((EventSourceId){argumentName})");
    }
}
