// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders optional snapshot query expectations directly from ESM.
/// </summary>
internal static class SemanticQuerySpecificationRenderer
{
    /// <summary>
    /// Renders one expected query result.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="expected">The expected query result.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source.</returns>
    public static RenderedFile Render(
        SemanticSpecification specification,
        SemanticSpecificationQueryResult expected,
        SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var readModel = context.ReadModels[query.ReadModel];
        var result = expected.Results.Single();
        var located = context.DeclaringSlice(specification.Id);
        var types = new SemanticTypeSystem(context);
        var behavior = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var queryNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path);
        var builder = new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Chronicle.ReadModels")
            .Using("Cratis.Specifications")
            .Using("NSubstitute")
            .Using("System.Globalization")
            .Using("Xunit")
            .Using($"{context.RootNamespace}.Common")
            .Using(queryNamespace);

        var expectedArguments = readModel.Properties.Select(property =>
            types.Value(result.Values.Single(_ => _.TargetProperty == property.Id).Value, property.Type));
        var key = types.Value(expected.Key, query.Argument.Type);
        builder.OpenBlock($"public class {behavior} : Specification")
            .Line("readonly IReadModels _readModels = Substitute.For<IReadModels>();")
            .Line($"readonly {readModelName} _expected = new({string.Join(", ", expectedArguments)});")
            .Line($"{readModelName}? _result;")
            .BlankLine()
            .Line($"void Establish() => _readModels.GetInstanceById<{readModelName}>((EventSourceId){key}).Returns(_expected);")
            .Line($"async Task Because() => _result = await {readModelName}.{Identifiers.ToPascalCase(query.Name)}(_readModels, {key});")
            .BlankLine()
            .Line("[Fact] void should_return_the_expected_read_model() => _result.ShouldEqual(_expected);")
            .EndBlock();

        var path = Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]);
        return new(path, Conditional(builder.ToString()));
    }

    static string Conditional(string content) => $"#if DEBUG\n{content}\n#endif\n";
}
