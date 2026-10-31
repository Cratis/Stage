// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Executes protected snapshot and observable queries through Arc's query pipeline and the fixture caller.
/// </summary>
internal static class SemanticProtectedQuerySpecificationRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticSpecificationQueryResult expected, SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var readModel = context.ReadModels[query.ReadModel];
        var located = context.DeclaringSlice(specification.Id);
        var name = SemanticSpecificationClassNames.Queried(specification, query, allowThrough: true);

        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var readModelName = types.SliceType(readModel.Id, readModel.Name);
        var builder = context.SpecificationBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{name}")
            .Using("Cratis.Arc.Chronicle.Testing.Queries")
            .Using("Cratis.Specifications")
            .Using("Microsoft.Extensions.DependencyInjection");

        var routedReplay = specification.GivenEvents.Any(given => given.Route is not null) ||
            SemanticSpecificationRouteRendering.RoutesCommand(specification, context);
        if (routedReplay)
        {
            builder.Using("Cratis.Chronicle.Testing.ReadModels");
        }
        context.Docs(specification.Id).Render(builder, $"Specifies {Identifiers.ToWords(specification.Name)}.")
            .OpenBlock($"public class {name} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Arc.Testing.Queries.QueryScenario<{readModelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line("readonly global::Cratis.Arc.Authorization.CurrentPrincipalAccessor _principalAccessor = new(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
            .Line("global::Cratis.Arc.Queries.QueryResult _result = null!;");
        if (routedReplay)
        {
            builder.Line($"readonly global::Cratis.Chronicle.Testing.ReadModels.ReadModelScenario<{readModelName}> _readModelScenario = new();");
        }
        builder.BlankLine()
            .OpenBlock(routedReplay ? "async global::System.Threading.Tasks.Task Establish()" : "void Establish()")
            .Line($"global::{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);")
            .Line("_scenario.Services.AddSingleton<global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor>(_principalAccessor);");
        Write(builder, Seeds(specification, expected, routedReplay, context, types));

        var caller = specification.GivenCaller!;
        var claims = caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
            .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
        var authentication = caller.Authenticated ? "\"Screenplay\"" : "null";
        builder.EndBlock().BlankLine()
            .OpenBlock("async global::System.Threading.Tasks.Task Because()")
            .Line($"var principal = new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
            .Line("using var scope = _principalAccessor.BeginScope(principal);")
            .Line($"_result = await _scenario.Perform(nameof({readModelName}.{context.MemberNames.Query(query)}), new global::Cratis.Arc.Queries.QueryArguments {{ {Arguments(expected, context, types)} }});")
            .EndBlock().BlankLine();

        Write(builder, Assertions(specification, expected, readModelName, context, types));
        RenderDispose(builder, routedReplay);
        builder.EndBlock();
        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), types.SpecificationContent(builder));
    }

    static string Key(SemanticSpecificationQueryResult expected, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var query = context.Queries[expected.Query];
        return query.Argument is { } argument ? types.Value(expected.Key, argument.Type) : string.Empty;
    }

    static string Arguments(SemanticSpecificationQueryResult expected, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var query = context.Queries[expected.Query];
        var key = query.Argument is { } argument
            ? new[] { $"[{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(argument.Name))}] = {Key(expected, context, types)}" }
            : [];
        return string.Join(", ", key.Concat(query.Filters.Select(filter =>
            $"[{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(filter.Name))}] = {types.Value(expected.Filters.SingleOrDefault(value => value.Filter == filter.Id)?.Value ?? SemanticValue.Null, filter.Type)}")));
    }

    static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> Replay(SemanticSpecification specification, SemanticReadModel readModel, SemanticApplicationContext context) =>
        specification.When is { } when
            ? SemanticProjectionSpecificationEvents.Replay(context.Application, specification, context.Projections.Values.Single(projection => projection.ReadModel == readModel.Id), context.Commands[when.Command])
            : [];

    static IEnumerable<string> Seeds(SemanticSpecification specification, SemanticSpecificationQueryResult expected, bool routedReplay, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var key = Key(expected, context, types);
        var replay = Replay(specification, context.ReadModels[context.Queries[expected.Query].ReadModel], context);
        var seed = routedReplay ? "await _readModelScenario" : "_scenario";
        foreach (var given in specification.GivenEvents)
        {
            var @event = context.Events[given.EventContract];
            var arguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            yield return $"{seed}.Given.ForEventSource({SemanticSpecificationEventSources.Expression(context, types, given)}){SemanticSpecificationRouteRendering.Seed(given.Route, context, types)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));";
        }

        if (specification.When is { } action)
        {
            var command = context.Commands[action.Command];
            foreach (var (produced, eventExpectation) in replay)
            {
                var @event = context.Events[produced.EventContract];
                var source = SemanticDestinations.ForSpecification(specification, command, produced);
                var route = SemanticSpecificationRouteRendering.Seed(SemanticSpecificationRouteRendering.ForCommand(specification, context, produced), context, types);
                var arguments = @event.Properties.Select(property =>
                    types.Value(eventExpectation.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
                yield return $"{seed}.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}){route}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));";
            }
        }

        if (routedReplay)
        {
            if (context.Queries[expected.Query].Cardinality == SemanticQueryCardinality.Many)
            {
                yield return "foreach (var instance in _readModelScenario.Instances)";
                yield return "{";
                yield return "    _scenario.Given.ForEventSource(instance.Key).ReadModel(instance.Value);";
                yield return "}";
            }
            else
            {
                yield return $"_scenario.Given.ForEventSource((global::Cratis.Chronicle.Events.EventSourceId){key}).ReadModel(_readModelScenario.InstanceForEventSourceId((global::Cratis.Chronicle.Events.EventSourceId){key})!);";
            }
        }
    }

    static IEnumerable<string> Assertions(SemanticSpecification specification, SemanticSpecificationQueryResult expected, string readModelName, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        if (specification.ThenDenied)
        {
            return
            [
                "[global::Xunit.FactAttribute] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();",
                "[global::Xunit.FactAttribute] void should_return_no_data() => _result.Data.ShouldBeNull();"
            ];
        }

        var readModel = context.ReadModels[context.Queries[expected.Query].ReadModel];
        if (context.Queries[expected.Query].Cardinality == SemanticQueryCardinality.Many)
        {
            return CollectionAssertions(expected, readModel, readModelName, context, types);
        }
        var comparisons = expected.Results.Single().Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
        {
            var property = readModel.Properties.Single(candidate => candidate.Id == value.TargetProperty);
            return $"[global::Xunit.FactAttribute] void should_have_{Identifiers.ToSnakeCase(property.Name)}() => (({readModelName})_result.Data!).{context.MemberNames.Property(property)}.ShouldEqual({types.Value(value.Value, property.Type)});";
        });
        return
        [
            "[global::Xunit.FactAttribute] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();",
            $"[global::Xunit.FactAttribute] void should_return_the_expected_read_model() => (_result.Data is {readModelName}).ShouldBeTrue();",
            .. comparisons
        ];
    }

    static IEnumerable<string> CollectionAssertions(SemanticSpecificationQueryResult expected, SemanticReadModel readModel, string readModelName, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        yield return "[global::Xunit.FactAttribute] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();";
        var models = $"global::System.Linq.Enumerable.Cast<{readModelName}>((global::System.Collections.Generic.IEnumerable<object>)_result.Data!)";
        yield return $"[global::Xunit.FactAttribute] void should_return_the_expected_count() => global::System.Linq.Enumerable.Count({models}).ShouldEqual({expected.Results.Length});";
        var key = readModel.Properties.Single(property => property.IsIdentifier);
        foreach (var (result, index) in expected.Results.Select((result, index) => (result, index)))
        {
            var selection = $"global::System.Linq.Enumerable.Single({models}, model => model.{context.MemberNames.Property(key)} == {types.Value(result.Key, key.Type)})";
            yield return $"[global::Xunit.FactAttribute] void should_return_result_{index + 1}() => ({selection} is not null).ShouldBeTrue();";
            foreach (var value in result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
            {
                var property = readModel.Properties.Single(property => property.Id == value.TargetProperty);
                yield return $"[global::Xunit.FactAttribute] void should_return_result_{index + 1}_with_{Identifiers.ToSnakeCase(property.Name)}() => {selection}.{context.MemberNames.Property(property)}.ShouldEqual({types.Value(value.Value, property.Type)});";
            }
        }
    }

    static void Write(CSharpCodeBuilder builder, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            builder.Line(line);
        }
    }

    static void RenderDispose(CSharpCodeBuilder builder, bool routedReplay)
    {
        if (routedReplay)
        {
            builder.OpenBlock("public void Dispose()").Line("_readModelScenario.Dispose();").Line("_scenario.Dispose();").EndBlock();
            return;
        }

        builder.Line("public void Dispose() => _scenario.Dispose();");
    }
}
