// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_validating_generation_rule_fixtures : Specification
{
    [Theory]
    [InlineData("provided", false)]
    [InlineData("different", true)]
    void should_check_concept_rules_after_accepting_the_string_shape(string requiredValue, bool refused)
    {
        var model = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan());
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Concepts = [.. model.Application.Concepts.Select(concept => concept.Generation?.Kind == "implementation"
                ? concept with { Validations = [new(default, SemanticValidationRuleKind.Equal, SemanticValue.Text(requiredValue), null)] }
                : concept)]
        });
        var options = new CratisRenderingOptions("GenerationFixtures", "GenerationFixtures");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var slice = context.SelectedSlices().Single().Slice;
        var specification = slice.Specifications.Single(specification => specification.Name == "GeneratorBodyNotExecuted");
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, slice with { Specifications = [specification] }, diagnostics);

        diagnostics.Exists(diagnostic => diagnostic.Code == "STAGE-ESM-011").ShouldEqual(refused);
        if (refused)
        {
            diagnostics.Single().Message.ShouldContain("Generated fixture 'value' fails concept 'TenantOrderId' rules");
        }
        else
        {
            diagnostics.ShouldBeEmpty();
        }
    }
}
