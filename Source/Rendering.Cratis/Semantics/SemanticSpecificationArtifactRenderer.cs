// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders one semantic specification into focused Cratis scenario-family artifacts.
/// </summary>
internal static class SemanticSpecificationArtifactRenderer
{
    /// <summary>
    /// Renders the generated specification artifacts.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source files.</returns>
    public static IReadOnlyList<RenderedFile> Render(
        SemanticSpecification specification,
        SemanticApplicationContext context)
    {
        specification = specification.WithPinnedActionValues();
        var sources = new[] { specification.Id, context.DeclaringSlice(specification.Id).Slice.Id };
        if (context.DeclaringSlice(specification.Id).Slice.Kind == SemanticSliceKind.Translate)
        {
            return [SemanticTranslationSpecificationRenderer.Render(specification, context) with { Sources = [.. sources] }];
        }
        if (SemanticProjectionFactSpecification.TryLower(context, specification, out var facts))
        {
            return [.. specification.ThenReadModels.Select(expected => SemanticReadModelSpecificationRenderer.Render(facts, expected, context) with { Sources = [.. sources] }),
                .. specification.ThenAbsentReadModels.Select((expected, index) => SemanticReadModelSpecificationRenderer.RenderAbsent(facts, expected, context, index) with { Sources = [.. sources] })];
        }
        if (SemanticReactionCascade.TryLower(context, specification, out var cascade) && SemanticSpecificationAdmission.CommandTriggersReaction(context, specification))
        {
            var reaction = SemanticReactionSpecificationRenderer.Render(cascade.Reaction, context) with { Sources = [.. sources] };

            return cascade.Trigger is null ? [reaction] : [reaction, SemanticCommandSpecificationRenderer.Render(cascade.Trigger, context) with { Sources = [.. sources] }];
        }
        if (SemanticSpecificationAdmission.AppendRefusedConstraint(context, specification) is { } refusing)
        {
            return [SemanticAppendRefusalRenderer.Render(specification, refusing, context) with { Sources = [.. sources] }];
        }
        if (specification.WhenAppended is not null || specification.WhenRedelivered is not null)
        {
            return [SemanticReactionSpecificationRenderer.Render(specification, context) with { Sources = [.. sources] }];
        }
        if (SemanticSpecificationAdmission.CanEmptyQueryOnly(specification, context))
        {
            return [SemanticEmptyQuerySpecificationRenderer.Render(specification, specification.ThenQueries[0], context) with { Sources = [specification.Id] }];
        }
        if (SemanticSpecificationAdmission.CanSeedQueryOnly(specification, context))
        {
            return [SemanticQuerySpecificationRenderer.RenderSeededQuery(specification, specification.ThenQueries[0], context) with { Sources = [.. sources] }];
        }

        if (SemanticSpecificationAdmission.CanDenyQueryOnly(specification, context) ||
            SemanticSpecificationAdmission.CanProjectQueryOnly(specification, context))
        {
            return [SemanticQuerySpecificationRenderer.Render(specification, specification.ThenQueries[0], context) with { Sources = [.. sources] }];
        }

        var files = new List<RenderedFile>
        {
            SemanticCommandSpecificationRenderer.Render(specification, context)
        };

        files.AddRange(specification.ThenReadModels.Select(_ =>
            SemanticReadModelSpecificationRenderer.Render(specification, _, context)));
        files.AddRange(specification.ThenAbsentReadModels.Select((expected, index) =>
            SemanticReadModelSpecificationRenderer.RenderAbsent(specification, expected, context, index)));
        files.AddRange(specification.ThenQueries.Select(_ =>
            SemanticQuerySpecificationRenderer.Render(specification, _, context)));
        return [.. files.Select(file => file with { Sources = [.. sources] })];
    }
}
