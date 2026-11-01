// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer;

public class when_rendering_requirements_with_validated_concepts : Specification
{
    string _check = null!;
    string _plain = null!;
    string _protected = null!;

    void Because()
    {
        var model = invoice_model.Compile("""
            concept Label : String
              validate
                min 2 message "Label short"
            concept Count : Int
              validate
                > 5 message "Count small"
            concept Tag : String
              validate
                not empty
            module Inputs
              feature Ordered
                slice StateChange Checking
                  command Check
                    id String
                    label Label
                    count Count optional
                    tag Tag
                    validate
                      label != "x" message "Command rule"
                      require id != "draft"
                        message "Requirement"
                    returns id
                slice StateChange Unrestricted
                  command Plain
                    id String
                    label Label
                    validate
                      label != "x" message "Plain rule"
                    returns id
            """);
        var artifacts = Plan(model);
        _check = artifacts.Single(artifact => artifact.Code.Contains("public class CheckValidator", StringComparison.Ordinal)).Code;
        _plain = artifacts.Single(artifact => artifact.Code.Contains("public class PlainValidator", StringComparison.Ordinal)).Code;
        _protected = string.Join('\n', Plan(SemanticModelSerializer.Deserialize(ProtectedCommandReadsCorpus.V25.EsmBytes.AsSpan())).Select(artifact => artifact.Code));
    }

    [Fact] void should_suppress_the_member_concept_validator() => _check.ShouldContain("RuleFor(_ => _.Label).IgnoreConceptRules();");
    [Fact] void should_suppress_the_optional_member_without_a_nullable_return_warning() => _check.ShouldContain("RuleFor(_ => _.Count!).IgnoreConceptRules();");
    [Fact] void should_render_command_rules_before_concept_rules() => _check.IndexOf("Command rule", StringComparison.Ordinal).ShouldBeLessThan(_check.IndexOf("Label short", StringComparison.Ordinal));
    [Fact] void should_render_concepts_in_property_declaration_order() => _check.IndexOf("Label short", StringComparison.Ordinal).ShouldBeLessThan(_check.IndexOf("Count small", StringComparison.Ordinal));
    [Fact] void should_render_concept_rules_before_requirements() => _check.IndexOf("Count small", StringComparison.Ordinal).ShouldBeLessThan(_check.IndexOf("RuleFor(_ => _).Must(command =>", StringComparison.Ordinal));
    [Fact] void should_accept_an_absent_optional_concept() => _check.ShouldContain("RuleFor<Count?>(_ => _.Count).Must(value => value is null || value.Value > 5m)");
    [Fact] void should_keep_the_default_concept_message() => _check.ShouldContain("WithMessage(\"A required concept value is empty.\")");
    [Fact] void should_leave_commands_without_requirements_alone() => _plain.ShouldNotContain("IgnoreConceptRules");
    [Fact] void should_leave_protected_commands_alone() => _protected.ShouldNotContain("IgnoreConceptRules");
    [Fact]
    void should_render_short_type_names_in_the_inlined_rules()
    {
        var start = _check.IndexOf("RuleFor(_ => _.Label).IgnoreConceptRules();", StringComparison.Ordinal);
        var end = _check.IndexOf("RuleFor(_ => _).Must(command =>", StringComparison.Ordinal);
        _check[start..end].ShouldNotContain("global::");
    }

    static (string RelativePath, string Code)[] Plan(ExecutableSemanticModel model)
    {
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ConceptOrder", "ConceptOrder"));
        return [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => (artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }
}
