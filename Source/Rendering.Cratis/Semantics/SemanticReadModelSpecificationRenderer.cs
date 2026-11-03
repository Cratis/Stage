// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders projected read-model expectations directly from ESM.
/// </summary>
internal static class SemanticReadModelSpecificationRenderer
{
    /// <summary>
    /// Renders one expected read-model state.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected read-model state.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticApplicationContext context)
        => Render(specification, expected, context, null);

    /// <summary>
    /// Renders the absence of one keyed read-model instance.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected absent instance.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="index">The expectation's position, to keep multiple absent keys distinct.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile RenderAbsent(
        SemanticSpecification specification,
        SemanticSpecificationAbsentReadModel expected,
        SemanticApplicationContext context,
        int index) => Render(specification, new(expected.ReadModel, expected.Key, []), context, index);

    static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationReadModel expected,
        SemanticApplicationContext context,
        int? absentIndex)
    {
        var readModel = context.ReadModels[expected.ReadModel];
        var projection = context.Projections.Values.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var reducer = context.Reducers.SingleOrDefault(_ => _.ReadModel == readModel.Id);
        var command = specification.When is null ? null : context.Commands[specification.When.Command];

        // Without a command there is nothing to replay: the facts are all given and the projection or reducer builds the model from them.
        IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> replay = [];
        if (command is not null)
        {
            replay = absentIndex is not null || !specification.GivenReadModels.IsEmpty ? SemanticReadModelSpecificationReplay.ForAbsence(context, specification, command)
                : SemanticReadModelSpecificationReplay.For(context.Application, specification, expected, new(projection, reducer, command));
        }
        var givenEvents = reducer is null ? SemanticProjectionSpecificationSeeds.Given(specification, projection!) : SemanticReducerSpecificationEvents.Given(specification, expected, reducer);
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var thenIndex = absentIndex is null ? specification.ThenReadModels.IndexOf(expected) : -1;
        var behavior = SemanticSpecificationClassNames.Projected(specification, readModel, absentIndex, thenIndex >= 0 ? thenIndex : null);
        var builder = Builder(behavior, located, readModel, context);
        var scope = new SemanticReadModelSpecificationScope(builder, types, context, located);
        var seeds = SemanticProjectionSpecificationSeeds.For(specification, readModel.Id).ToArray();
        var readModelName = types.SliceType(readModel.Id, readModel.Name);

        context.Docs(specification.Id).Render(builder, $"Specifies {Identifiers.ToWords(specification.Name)}.")
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .BlankLine()
            .OpenBlock(givenEvents.Count == 0 && replay.Count == 0 && seeds.Length == 0 ? "void Establish()" : "async global::System.Threading.Tasks.Task Establish()");
        if (seeds.Length > 0) SemanticProjectionSpecificationSeeds.Render(builder, specification, readModel, types);
        SemanticReadModelSpecificationGivens.Render(scope, givenEvents);
        SemanticReadModelSpecificationReplay.Render(scope, specification, command, replay);

        builder.EndBlock()
            .BlankLine();
        SemanticReadModelSpecificationExpectations.Render(scope, new(readModel, expected, reducer is not null, absentIndex));

        builder.Line("/// <inheritdoc/>").Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);

        return new(path, types.SpecificationContent(builder));
    }

    static CSharpCodeBuilder Builder(
        string behavior,
        LocatedSemanticSlice located,
        SemanticReadModel readModel,
        SemanticApplicationContext context)
    {
        return context.SpecificationBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Testing.ReadModels")
            .Using("Cratis.Specifications");
    }
}
