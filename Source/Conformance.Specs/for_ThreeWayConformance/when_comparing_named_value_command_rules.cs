// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_named_value_command_rules(when_comparing_named_value_command_rules.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_named_value_command_rules.context>
{
    [Fact] void should_cover_required_optional_numbered_members_messages_severities_and_requirements() => fixture.Outcomes.Length.ShouldEqual(52);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_every_rendered_specification() => fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "NamedValues";
        protected override string ProjectFile => "NamedValues.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", named_value_command_cases.Compile())];

        async Task Because()
        {
            var plan = SemanticExecutionPlan.Compile(named_value_command_cases.Compile()).Plan!;
            var probes = plan.Specifications.Values.Select(specification =>
            {
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(reference.Passed, string.Join(';', reference.Failures));
                var failures = reference.Execution is SemanticRejected rejected
                    ? rejected.ValidationFailures.Select(failure => new[] { failure.Message, failure.Severity.ToString() }).ToArray()
                    : [];
                Assert.Equal(specification.Name.EndsWith("Invalid", StringComparison.Ordinal), failures.Length > 0);
                var command = plan.Commands[specification.When!.Command];
                var input = specification.When.Values.Single(value => command.Properties.Single(property => property.Id == value.TargetProperty).Name == "value").Value;
                var member = input is SemanticTextValue text ? Identifiers.ToPascalCase(text.Value) : null;
                return $$"""
                    {
                        var commandType = typeof(NamedValues.Common.Status).Assembly.GetTypes().Single(type => type.Name == {{JsonSerializer.Serialize(command.Name)}});
                        var constructor = commandType.GetConstructors().Single();
                        var valueType = constructor.GetParameters().Single(parameter => parameter.Name == "Value").ParameterType;
                        var enumType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                        string? member = {{JsonSerializer.Serialize(member)}};
                        var value = member is null ? null : Enum.Parse(enumType, member);
                        using var scenario = new Cratis.Arc.Testing.Commands.CommandScenario<object>();
                        using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                        var result = await scenario.Execute(constructor.Invoke(["one", value, Enum.Parse(enumType, "InReview")]));
                        Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                        var expected = System.Text.Json.JsonSerializer.Deserialize<string[][]>({{JsonSerializer.Serialize(JsonSerializer.Serialize(failures))}})!;
                        Assert.Equal(expected.Select(pair => (pair[0], pair[1])), result.ValidationResults.Select(failure => (failure.Message, failure.Severity.ToString())));
                    }
                    """;
            });
            AddGeneratedSpecification("NamedValueFailures.cs", $$"""
                #if DEBUG
                using Xunit;
                namespace NamedValues.ValidationParity;
                public class NamedValueFailures
                {
                    [Fact]
                    public async Task should_match_reference_messages_and_severities()
                    {
                        {{string.Join('\n', probes)}}
                    }
                }
                #endif
                """);
            await Verify();
        }
    }
}
#endif
