// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Runs query-only rejection and empty-world expectations through Arc's query pipeline.
/// </summary>
internal static class SemanticEmptyQuerySpecificationRenderer
{
    internal static RenderedFile Render(SemanticSpecification specification, SemanticSpecificationQueryResult expected, SemanticApplicationContext context)
    {
        var query = context.Queries[expected.Query];
        var readModel = context.ReadModels[query.ReadModel];
        var types = new SemanticTypeSystem(context) { SpecificationValues = new(context, specification) };
        var modelName = types.SliceType(readModel.Id, readModel.Name);
        var located = context.DeclaringSlice(specification.Id);
        var behavior = SemanticSpecificationClassNames.Queried(specification, query, allowThrough: false);
        var builder = context.SpecificationBuilder().Namespace($"{SliceNaming.Namespace(context.RootNamespace, located.Path)}.{behavior}")
            .Using("Cratis.Specifications").Using("Microsoft.Extensions.DependencyInjection").Using("NSubstitute").Using("Xunit");
        context.Docs(specification.Id).Render(builder)
            .OpenBlock($"public class {behavior} : global::Cratis.Specifications.Specification, global::System.IDisposable")
            .Line($"readonly global::Cratis.Arc.Testing.Queries.QueryScenario<{modelName}> _scenario = new();")
            .Line(SemanticSpecificationValuesRenderer.Declarations)
            .Line("global::Cratis.Arc.Queries.QueryResult _result = null!;");

        // A query without modeled authorization renders [Authorize], so a specification without a caller runs as the default authenticated caller.
        var carriesCaller = specification.GivenCaller is not null || query.Authorization is null;
        if (carriesCaller)
        {
            builder.Line("readonly global::Cratis.Arc.Authorization.CurrentPrincipalAccessor _principalAccessor = new(new global::Cratis.Arc.Http.HttpRequestContextAccessor());")
                .Line("global::System.Security.Claims.ClaimsPrincipal _principal = null!;");
        }
        builder.BlankLine().OpenBlock("void Establish()");

        // The fixture world is empty. Substitute only storage, leaving conversion, authorization and validation in Arc.
        builder.Line("var readModels = global::NSubstitute.Substitute.For<global::Cratis.Chronicle.ReadModels.IReadModels>();")
            .Line($"readModels.GetInstanceById<{modelName}>(global::NSubstitute.Arg.Any<global::Cratis.Chronicle.ReadModels.ReadModelKey>(), global::NSubstitute.Arg.Any<global::Cratis.Chronicle.ReadModels.ReadModelSessionId?>()).Returns(global::System.Threading.Tasks.Task.FromResult<{modelName}>(null!));")
            .Line($"readModels.GetInstances<{modelName}>(global::NSubstitute.Arg.Any<global::Cratis.Chronicle.Events.EventCount?>()).Returns(global::System.Threading.Tasks.Task.FromResult<global::System.Collections.Generic.IEnumerable<{modelName}>>([]));")
            .Line("_scenario.Services.AddSingleton<global::Cratis.Chronicle.ReadModels.IReadModels>(readModels);");
        if (carriesCaller)
        {
            var caller = specification.GivenCaller;
            if (caller is null)
            {
                builder.Line(SemanticDefaultCaller.Comment);
            }
            var claims = caller is null ? [] : caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
                .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));
            builder.Line($"_principal = new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([{string.Join(", ", claims)}], {(caller?.Authenticated != false ? "\"Screenplay\"" : "null")}));")
                .Line("_scenario.Services.AddSingleton<global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor>(_principalAccessor);");
        }
        if (query.Authorization is not null)
        {
            builder.Line($"global::{context.RootNamespace}.GeneratedPolicies.Registration.Register(_scenario.Services);");
        }
        if (query.Delivery == SemanticQueryDelivery.Live)
        {
            builder.Line($"readModels.Observe<{modelName}>(global::NSubstitute.Arg.Any<global::System.Linq.Expressions.Expression<global::System.Func<{modelName}, bool>>?>()).Returns(new global::System.Reactive.Subjects.BehaviorSubject<global::System.Collections.Generic.IEnumerable<{modelName}>>([]));");
        }
        var key = query.Argument is { } argument ? new[] { Argument(argument.Name, types.Value(expected.Key, argument.Type)) } : [];
        var filters = query.Filters.Where(filter => expected.Filters.Any(value => value.Filter == filter.Id))
            .Select(filter => Argument(filter.Name, types.Value(expected.Filters.Single(value => value.Filter == filter.Id).Value, filter.Type)));
        builder.EndBlock().BlankLine().OpenBlock("async global::System.Threading.Tasks.Task Because()");
        if (carriesCaller) builder.Line("using var scope = _principalAccessor.BeginScope(_principal);");
        builder.Line($"_result = await _scenario.Perform(nameof({modelName}.{context.MemberNames.Query(query)}), new global::Cratis.Arc.Queries.QueryArguments {{ {string.Join(", ", key.Concat(filters))} }});")
            .EndBlock().BlankLine().Line("[global::Xunit.FactAttribute] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();")
            .Line("[global::Xunit.FactAttribute] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();");
        if (specification.ThenErrors is [var error])
        {
            builder.Line("[global::Xunit.FactAttribute] void should_reject_the_query() => _result.IsValid.ShouldBeFalse();")
                .Line("[global::Xunit.FactAttribute] void should_return_no_data() => _result.Data.ShouldBeNull();")
                .Line("[global::Xunit.FactAttribute] void should_report_validation_failures() => _result.ValidationResults.ShouldNotBeEmpty();");
            if (error.Message is { } message)
            {
                var member = message.StartsWith("$strings.", StringComparison.Ordinal) ? "State" : "Message";
                builder.Line($"[global::Xunit.FactAttribute] void should_report_the_expected_first_error() => global::System.Linq.Enumerable.First(_result.ValidationResults).{member}.ShouldEqual({CSharpCodeBuilder.StringLiteral(message)});");
            }
        }
        else
        {
            builder.Line("[global::Xunit.FactAttribute] void should_accept_the_query() => _result.IsValid.ShouldBeTrue();")
                .Line(query.Cardinality == SemanticQueryCardinality.Many
                    ? "[global::Xunit.FactAttribute] void should_return_no_rows() => (_result.Data is global::System.Collections.Generic.IEnumerable<object> rows && !global::System.Linq.Enumerable.Any(rows)).ShouldBeTrue();"
                    : "[global::Xunit.FactAttribute] void should_return_no_data() => _result.Data.ShouldBeNull();");
        }
        builder.Line("public void Dispose() => _scenario.Dispose();").EndBlock();

        return new(Path.Combine([.. SliceNaming.FolderPath(located.Path), $"{behavior}.cs"]), types.SpecificationContent(builder));
    }

    static string Argument(string name, string value) => $"[{CSharpCodeBuilder.StringLiteral(Identifiers.ToCamelCase(name))}] = {value}";
}
