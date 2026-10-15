// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_generated_concept_rules : Specification
{
    const string Pin = "          for \"cus_01arz3ndektsv4rrffq69g5fav\"";

    [Fact]
    void should_render_the_factory_and_concept_validator_with_a_passing_pin()
    {
        var plan = Plan(Pin);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Text(plan, "Common/CustomerId.cs").ShouldContain("GenerateValue.Ulid(context)");
        Text(plan, "Common/CustomerId.cs").ShouldContain("ConceptValidator<CustomerId>");
        Text(plan, "Common/CustomerId.cs").ShouldContain("MatchesPattern(");
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("information")]
    void should_render_non_error_severity_rules(string severity)
    {
        var model = Model(Pin);
        var concept = model.Application.Concepts.Single();
        var ruleSeverity = Enum.Parse<SemanticValidationSeverity>(severity, true);
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Concepts = [concept with { Validations = [.. concept.Validations.Select(rule => rule with { Severity = ruleSeverity })] }]
        });
        var plan = Render(model);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Text(plan, "Common/CustomerId.cs").ShouldContain("ConceptValidator<CustomerId>");
    }

    [Fact]
    void should_require_a_pin_only_for_the_rule_bearing_specification()
    {
        var model = Model(string.Empty);
        var concept = model.Application.Concepts.Single();
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Concepts = [concept with { Validations = [concept.Validations.Single() with { Operand = new SemanticTextValue("^(?!never$)cus_[0-7][0-9a-hjkmnp-tv-z]{25}$") }] }]
        });
        var plan = Render(model);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        var specification = SemanticExecutionPlan.Compile(model).Plan!.Specifications.Values.Single();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-038" && diagnostic.Artifact == specification.Id);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [feature.Slices.Single() with { Specifications = [] }] }] }]
        });
        var commandPlan = Render(model);
        Assert.True(commandPlan.Success, string.Join(';', commandPlan.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_render_unpinned_generation_with_proven_rules()
    {
        var plan = Plan(string.Empty);
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Text(plan, "Common/CustomerId.cs").ShouldContain("ConceptValidator<CustomerId>");
    }

    [Fact]
    void should_require_a_pin_if_a_rule_is_refuted_after_validation()
    {
        var model = Model(string.Empty);
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var concept = model.Application.Concepts.Single();

        // Validated models reject refuted rules. Inject one after validation to exercise Stage's fail-closed proof check.
        typeof(SemanticConcept).GetField("<Validations>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(concept, ImmutableArray.Create(concept.Validations.Single() with { Operand = new SemanticTextValue("^impossible$") }));
        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("RuleApp", "RuleApp"));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-038" && diagnostic.Message.Contains("pinned", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_a_shape_valid_fixture_that_fails_a_rule()
    {
        var model = invoice_model.Compile("""
            concept Code : String
              generate nanoid 64 alphabet "ab"
              validate
                matches "^(?!a{63}b$)[ab]{64}$"
            module M
              feature F
                slice StateChange S
                  command C
                    code Code generated
                    returns code
                  specification FailingPin
                    when C
                      generated code = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab"
                    then error "Generated fixture 'code' fails concept 'Code' rules"
            """);
        var plan = Render(model);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("fails concept 'Code' rules", StringComparison.Ordinal));
    }

    static ExecutableSemanticModel Model(string pin) => invoice_model.Compile($$"""
        concept CustomerId : String
          generate ulid prefix "cus_" case lower
          validate
            matches "^cus_[0-7][0-9a-hjkmnp-tv-z]{25}$"
        module Shop
          feature Customers
            slice StateChange Register
              command Register
                id CustomerId generated identifier
                produces event Registered
                returns id
              specification Registering
                when Register
        {{pin}}
                then Registered
        """);

    static ArtifactRenderPlan Plan(string pin) => Render(Model(pin));
    static ArtifactRenderPlan Render(ExecutableSemanticModel model) => CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("RuleApp", "RuleApp"));
    static string Text(ArtifactRenderPlan plan, string path) => Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
