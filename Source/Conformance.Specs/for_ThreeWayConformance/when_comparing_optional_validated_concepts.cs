// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_optional_validated_concepts(when_comparing_optional_validated_concepts.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_optional_validated_concepts.context>
{
    [Fact] void should_cover_absent_valid_and_invalid_whole_decimal_and_text_concepts() => fixture.Outcomes.Length.ShouldEqual(9);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_pass_every_rendered_specification() => fixture.Outcomes.All(outcome => outcome.EndsWith("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "OptionalInputs";
        protected override string ProjectFile => "OptionalInputs.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("typed", optional_validated_concept_cases.Compile())];

        async Task Because()
        {
            var plan = SemanticExecutionPlan.Compile(optional_validated_concept_cases.Compile()).Plan!;
            var probes = plan.Specifications.Values.Select(specification =>
            {
                var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
                Assert.True(reference.Passed, string.Join(';', reference.Failures));
                var failures = reference.Execution is SemanticRejected rejected
                    ? rejected.ValidationFailures.Select(failure => new[] { failure.Message, failure.Severity.ToString() }).ToArray()
                    : [];
                var value = specification.When!.Values.Single(value => plan.Commands[specification.When.Command].Properties.Single(property => property.Id == value.TargetProperty).Name == "value").Value;
                var text = value switch
                {
                    SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
                    SemanticTextValue literal => literal.Value,
                    _ => null
                };
                return $$"""
                    {
                        var commandType = typeof(OptionalInputs.Common.Count).Assembly.GetTypes().Single(type => type.Name == {{JsonSerializer.Serialize(plan.Commands[specification.When.Command].Name)}});
                        var constructor = commandType.GetConstructors().Single();
                        var valueType = constructor.GetParameters().Single(parameter => parameter.Name == "Value").ParameterType;
                        var valueConstructor = valueType.GetConstructors().Single(constructor => constructor.GetParameters().Length == 1);
                        string? input = {{JsonSerializer.Serialize(text)}};
                        var value = input is null ? null : valueConstructor.Invoke([Convert.ChangeType(input, valueConstructor.GetParameters()[0].ParameterType, System.Globalization.CultureInfo.InvariantCulture)]);
                        using var scenario = new Cratis.Arc.Testing.Commands.CommandScenario<object>();
                        using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                        var result = await scenario.Execute(constructor.Invoke(["one", value]));
                        Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                        var expected = System.Text.Json.JsonSerializer.Deserialize<string[][]>({{JsonSerializer.Serialize(JsonSerializer.Serialize(failures))}})!;
                        Assert.Equal(expected.Select(pair => (pair[0], pair[1])), result.ValidationResults.Select(failure => (failure.Message, failure.Severity.ToString())));
                    }
                    """;
            });
            AddGeneratedSpecification("OrderedConceptFailures.cs", $$"""
                #if DEBUG
                using Xunit;
                namespace OptionalInputs.ValidationParity;
                public class OrderedConceptFailures
                {
                    [Fact]
                    public async Task should_match_reference_messages_severities_and_order()
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
