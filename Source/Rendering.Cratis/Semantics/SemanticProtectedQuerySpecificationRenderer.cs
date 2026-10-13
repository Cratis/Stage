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
        var queryNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(query.Id).Path);
        var located = context.DeclaringSlice(specification.Id);
        var name = $"when_{Identifiers.ToSnakeCase(specification.Name)}_is_queried";
        if (specification.ThenQueries.Length > 1)
        {
            name += $"_through_{Identifiers.ToSnakeCase(query.Name)}";
        }

        var readModelName = new SemanticTypeSystem(context).SliceType(readModel.Id, readModel.Name);
        var builder = new CSharpCodeBuilder()
            .Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{name}")
            .Using("System.Security.Claims")
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Chronicle.Testing.Queries")
            .Using("Cratis.Arc.Http")
            .Using("Cratis.Arc.Queries")
            .Using("Cratis.Arc.Testing.Queries")
            .Using("Cratis.Chronicle.Events")
            .Using("Cratis.Specifications")
            .Using("Microsoft.Extensions.DependencyInjection")
            .Using("Xunit")
            .Using(context.CommonNamespace)
            .Using(queryNamespace);

        var replay = Replay(specification, readModel, context);
        UseEventNamespaces(builder, [.. replay.Select(_ => _.Produced.EventContract), .. specification.GivenEvents.Select(given => given.EventContract)], queryNamespace, context);

        var routedReplay = specification.GivenEvents.Any(given => given.Route is not null) ||
            SemanticSpecificationRouteRendering.ForCommand(specification, context) is not null;
        if (routedReplay)
        {
            builder.Using("Cratis.Chronicle.Testing.ReadModels");
        }
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {name} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Arc.Testing.Queries.QueryScenario<{readModelName}> _scenario = new();")
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
        Write(builder, Seeds(specification, expected, routedReplay, context));

        var caller = specification.GivenCaller!;
        var claims = caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
            .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
        var authentication = caller.Authenticated ? "\"Screenplay\"" : "null";
        builder.EndBlock().BlankLine()
            .OpenBlock("async global::System.Threading.Tasks.Task Because()")
            .Line($"var principal = new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
            .Line("using var scope = _principalAccessor.BeginScope(principal);")
            .Line($"_result = await _scenario.Perform(nameof({readModelName}.{Identifiers.ToPascalCase(query.Name)}), new global::Cratis.Arc.Queries.QueryArguments {{ {Arguments(expected, context)} }});")
            .EndBlock().BlankLine();

        Write(builder, Assertions(specification, expected, readModelName, context));
        RenderDispose(builder, routedReplay);
        builder.EndBlock();
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), $"#if DEBUG\n{content}\n#endif\n");
    }

    static void UseEventNamespaces(CSharpCodeBuilder builder, IEnumerable<SemanticId> events, string queryNamespace, SemanticApplicationContext context)
    {
        foreach (var eventNamespace in events.Select(@event => SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(@event).Path))
            .Where(eventNamespace => !string.Equals(eventNamespace, queryNamespace, StringComparison.Ordinal)))
        {
            builder.Using(eventNamespace);
        }
    }

    static string Key(SemanticSpecificationQueryResult expected, SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        return query.Argument is { } argument ? new SemanticTypeSystem(context).QueryArgumentValue(expected.Key, argument.Type) : string.Empty;
    }

    static string Arguments(SemanticSpecificationQueryResult expected, SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var types = new SemanticTypeSystem(context);
        var key = query.Argument is { } argument
            ? new[] { $"[{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(argument.Name))}] = {Key(expected, context)}" }
            : [];
        return string.Join(", ", key.Concat(query.Filters.Select(filter =>
            $"[{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(filter.Name))}] = {types.QueryArgumentValue(expected.Filters.SingleOrDefault(value => value.Filter == filter.Id)?.Value ?? SemanticValue.Null, filter.Type)}")));
    }

    static IReadOnlyList<(SemanticProducedEvent Produced, SemanticSpecificationEvent Expected)> Replay(SemanticSpecification specification, SemanticReadModel readModel, SemanticApplicationContext context) =>
        specification.When is { } when
            ? SemanticProjectionSpecificationEvents.Replay(specification, context.Projections.Values.Single(projection => projection.ReadModel == readModel.Id), context.Commands[when.Command])
            : [];

    static IEnumerable<string> Seeds(SemanticSpecification specification, SemanticSpecificationQueryResult expected, bool routedReplay, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var key = Key(expected, context);
        var replay = Replay(specification, context.ReadModels[context.Queries[expected.Query].ReadModel], context);
        var seed = routedReplay ? "await _readModelScenario" : "_scenario";
        foreach (var given in specification.GivenEvents)
        {
            var @event = context.Events[given.EventContract];
            var arguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            var source = given.EventSource!;
            yield return $"{seed}.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}){SemanticSpecificationRouteRendering.Seed(given.Route, context)}.Events(new {types.EventType(@event)}({string.Join(", ", arguments)}));";
        }

        if (specification.When is { } action)
        {
            var command = context.Commands[action.Command];
            var route = SemanticSpecificationRouteRendering.Seed(SemanticSpecificationRouteRendering.ForCommand(specification, context), context);
            foreach (var (produced, eventExpectation) in replay)
            {
                var @event = context.Events[produced.EventContract];
                var source = SemanticDestinations.ForSpecification(specification, command, produced);
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

    static IEnumerable<string> Assertions(SemanticSpecification specification, SemanticSpecificationQueryResult expected, string readModelName, SemanticApplicationContext context)
    {
        if (specification.ThenDenied)
        {
            return
            [
                "[global::Xunit.FactAttribute] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();",
                "[global::Xunit.FactAttribute] void should_return_no_data() => _result.Data.ShouldBeNull();"
            ];
        }

        var types = new SemanticTypeSystem(context);
        var readModel = context.ReadModels[context.Queries[expected.Query].ReadModel];
        if (context.Queries[expected.Query].Cardinality == SemanticQueryCardinality.Many)
        {
            var predicates = expected.Results.Select(result =>
            {
                var rowComparisons = result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
                {
                    var property = readModel.Properties.Single(candidate => candidate.Id == value.TargetProperty);
                    return $"model.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value.Value, property.Type)}";
                });
                return $"global::System.Linq.Enumerable.Any(models, row => row is {readModelName} model && {string.Join(" && ", rowComparisons)})";
            });
            var predicate = string.Join(" && ", new[] { $"global::System.Linq.Enumerable.Count(models) == {expected.Results.Length}" }.Concat(predicates));

            return
            [
                "[global::Xunit.FactAttribute] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();",
                $"[global::Xunit.FactAttribute] void should_return_the_expected_rows() => (_result.Data is global::System.Collections.Generic.IEnumerable<object> models && {predicate}).ShouldBeTrue();"
            ];
        }
        var comparisons = expected.Results.Single().Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
        {
            var property = readModel.Properties.Single(candidate => candidate.Id == value.TargetProperty);
            return $"model.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value.Value, property.Type)}";
        });
        return
        [
            "[global::Xunit.FactAttribute] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();",
            $"[global::Xunit.FactAttribute] void should_return_the_expected_read_model() => (_result.Data is {readModelName} model{string.Concat(comparisons.Select(value => $" && {value}"))}).ShouldBeTrue();"
        ];
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
