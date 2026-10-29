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
        SemanticApplicationContext context) =>
        [.. Dispatch(specification, context).Select(entry => entry.Render() with { Sources = [.. entry.Sources] })];

    /// <summary>
    /// Gets the names of the specification classes <see cref="Render"/> emits, without rendering them.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The specification class names.</returns>
    internal static IReadOnlyList<string> ClassNames(
        SemanticSpecification specification,
        SemanticApplicationContext context) =>
        [.. Dispatch(specification, context).Select(entry => entry.Name)];

    static List<(string Name, Func<RenderedFile> Render, SemanticId[] Sources)> Dispatch(
        SemanticSpecification specification,
        SemanticApplicationContext context)
    {
        specification = specification.WithPinnedActionValues();
        var sources = new[] { specification.Id, context.DeclaringSlice(specification.Id).Slice.Id };
        var entries = new List<(string Name, Func<RenderedFile> Render, SemanticId[] Sources)>();
        var spec = specification;
        void Add(string name, Func<RenderedFile> render, SemanticId[]? entrySources = null) => entries.Add((name, render, entrySources ?? sources));

        if (context.DeclaringSlice(spec.Id).Slice.Kind == SemanticSliceKind.Translate)
        {
            Add(SemanticSpecificationClassNames.Behavior(spec), () => SemanticTranslationSpecificationRenderer.Render(spec, context));
        }
        else if (SemanticProjectionFactSpecification.TryLower(context, spec, out var facts))
        {
            foreach (var (expected, thenIndex) in spec.ThenReadModels.Select((expected, thenIndex) => (expected, thenIndex)))
            {
                Add(SemanticSpecificationClassNames.Projected(facts, context.ReadModels[expected.ReadModel], null, thenIndex), () => SemanticReadModelSpecificationRenderer.Render(facts, expected, context));
            }
            foreach (var (expected, index) in spec.ThenAbsentReadModels.Select((expected, index) => (expected, index)))
            {
                Add(SemanticSpecificationClassNames.Projected(facts, context.ReadModels[expected.ReadModel], index), () => SemanticReadModelSpecificationRenderer.RenderAbsent(facts, expected, context, index));
            }
        }
        else if (SemanticReactionCascade.TryLower(context, spec, out var cascade) && SemanticSpecificationAdmission.CommandTriggersReaction(context, spec))
        {
            Add(SemanticSpecificationClassNames.Behavior(cascade.Reaction), () => SemanticReactionSpecificationRenderer.Render(cascade.Reaction, context));
            if (cascade.Trigger is not null)
            {
                Add(SemanticSpecificationClassNames.Behavior(cascade.Trigger), () => SemanticCommandSpecificationRenderer.Render(cascade.Trigger, context));
            }
        }
        else if (SemanticSpecificationAdmission.AppendRefusedConstraint(context, spec) is { } refusing)
        {
            Add(SemanticSpecificationClassNames.Behavior(spec), () => SemanticAppendRefusalRenderer.Render(spec, refusing, context));
        }
        else if (spec.WhenAppended is not null || spec.WhenRedelivered is not null)
        {
            Add(SemanticSpecificationClassNames.Behavior(spec), () => SemanticReactionSpecificationRenderer.Render(spec, context));
        }
        else if (SemanticSpecificationAdmission.CanEmptyQueryOnly(spec, context))
        {
            var query = context.Queries[spec.ThenQueries[0].Query];
            Add(SemanticSpecificationClassNames.Queried(spec, query, allowThrough: false), () => SemanticEmptyQuerySpecificationRenderer.Render(spec, spec.ThenQueries[0], context), [spec.Id]);
        }
        else if (SemanticSpecificationAdmission.CanSeedQueryOnly(spec, context))
        {
            var query = context.Queries[spec.ThenQueries[0].Query];
            Add(SemanticSpecificationClassNames.Queried(spec, query, allowThrough: false), () => SemanticQuerySpecificationRenderer.RenderSeededQuery(spec, spec.ThenQueries[0], context));
        }
        else if (SemanticSpecificationAdmission.CanDenyQueryOnly(spec, context) || SemanticSpecificationAdmission.CanProjectQueryOnly(spec, context))
        {
            var query = context.Queries[spec.ThenQueries[0].Query];
            Add(SemanticSpecificationClassNames.Queried(spec, query, allowThrough: true), () => SemanticQuerySpecificationRenderer.Render(spec, spec.ThenQueries[0], context));
        }
        else
        {
            AddDefault(spec, context, Add);
        }

        return entries;
    }

    static void AddDefault(SemanticSpecification spec, SemanticApplicationContext context, Action<string, Func<RenderedFile>, SemanticId[]?> add)
    {
        add(SemanticSpecificationClassNames.Behavior(spec), () => SemanticCommandSpecificationRenderer.Render(spec, context), null);
        foreach (var (expected, thenIndex) in spec.ThenReadModels.Select((expected, thenIndex) => (expected, thenIndex)))
        {
            add(SemanticSpecificationClassNames.Projected(spec, context.ReadModels[expected.ReadModel], null, thenIndex), () => SemanticReadModelSpecificationRenderer.Render(spec, expected, context), null);
        }
        foreach (var (expected, index) in spec.ThenAbsentReadModels.Select((expected, index) => (expected, index)))
        {
            add(SemanticSpecificationClassNames.Projected(spec, context.ReadModels[expected.ReadModel], index), () => SemanticReadModelSpecificationRenderer.RenderAbsent(spec, expected, context, index), null);
        }
        foreach (var expected in spec.ThenQueries)
        {
            add(SemanticSpecificationClassNames.Queried(spec, context.Queries[expected.Query], allowThrough: true), () => SemanticQuerySpecificationRenderer.Render(spec, expected, context), null);
        }
    }
}
