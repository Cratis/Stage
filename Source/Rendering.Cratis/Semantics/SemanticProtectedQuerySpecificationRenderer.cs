// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Executes protected snapshot queries through Arc's query pipeline and the fixture caller.
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

        var readModelName = Identifiers.ToPascalCase(readModel.Name);
        var types = new SemanticTypeSystem(context);
        var key = types.Value(expected.Key, query.Argument.Type);
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
            .Using($"{context.RootNamespace}.Common")
            .Using(queryNamespace);

        var replay = specification.When is { } when
            ? SemanticProjectionSpecificationEvents.Replay(specification, context.Projections.Values.Single(projection => projection.ReadModel == readModel.Id), context.Commands[when.Command])
            : [];
        foreach (var (produced, _) in replay)
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(produced.EventContract).Path);
            if (!string.Equals(eventNamespace, queryNamespace, StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        foreach (var given in specification.GivenEvents)
        {
            var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(given.EventContract).Path);
            if (!string.Equals(eventNamespace, queryNamespace, StringComparison.Ordinal))
            {
                builder.Using(eventNamespace);
            }
        }

        builder.OpenBlock($"public class {name} : Specification, IDisposable")
            .Line($"readonly QueryScenario<{readModelName}> _scenario = new();")
            .Line("readonly CurrentPrincipalAccessor _principalAccessor = new(new HttpRequestContextAccessor());")
            .Line("QueryResult _result = null!;")
            .BlankLine()
            .OpenBlock("void Establish()")
            .Line($"global::{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);")
            .Line("_scenario.Services.AddSingleton<ICurrentPrincipalAccessor>(_principalAccessor);");
        foreach (var given in specification.GivenEvents)
        {
            var @event = context.Events[given.EventContract];
            var arguments = @event.Properties.Select(property =>
                types.Value(given.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
            var source = given.EventSource!;
            builder.Line($"_scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}).Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", arguments)}));");
        }

        if (specification.When is { } action)
        {
            var command = context.Commands[action.Command];
            foreach (var (produced, eventExpectation) in replay)
            {
                var @event = context.Events[produced.EventContract];
                var source = SemanticDestinations.ForSpecification(specification, command, produced);
                var arguments = @event.Properties.Select(property =>
                    types.Value(eventExpectation.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type));
                builder.Line($"_scenario.Given.ForEventSource({types.EventSourceExpression(types.Value(source.Value, source.Type), source.Type)}).Events(new {Identifiers.ToPascalCase(@event.Name)}({string.Join(", ", arguments)}));");
            }
        }

        var caller = specification.GivenCaller!;
        var claims = caller.Roles.Select(role => $"new Claim(ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
            .Concat(caller.Claims.Select(claim => $"new Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
        var authentication = caller.Authenticated ? "\"Screenplay\"" : "null";
        builder.EndBlock().BlankLine()
            .OpenBlock("async Task Because()")
            .Line($"var principal = new ClaimsPrincipal(new ClaimsIdentity([{string.Join(", ", claims)}], {authentication}));")
            .Line("using var scope = _principalAccessor.BeginScope(principal);")
            .Line($"_result = await _scenario.Perform(nameof({readModelName}.{Identifiers.ToPascalCase(query.Name)}), new QueryArguments {{ [{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(query.Argument.Name))}] = {key} }});")
            .EndBlock().BlankLine();

        if (specification.ThenDenied)
        {
            builder.Line("[Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();")
                .Line("[Fact] void should_return_no_data() => _result.Data.ShouldBeNull();");
        }
        else
        {
            var result = expected.Results.Single();
            var comparisons = result.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal).Select(value =>
            {
                var property = readModel.Properties.Single(candidate => candidate.Id == value.TargetProperty);
                return $"model.{Identifiers.ToPascalCase(property.Name)} == {types.Value(value.Value, property.Type)}";
            });
            builder.Line("[Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();")
                .Line($"[Fact] void should_return_the_expected_read_model() => (_result.Data is {readModelName} model{string.Concat(comparisons.Select(value => $" && {value}"))}).ShouldBeTrue();");
        }

        builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();
        var content = builder.ToString();
        if (SpecificationValues.NeedsGlobalization(content))
        {
            content = builder.Using("System.Globalization").ToString();
        }

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{name}.cs"]), $"#if DEBUG\n{content}\n#endif\n");
    }
}
