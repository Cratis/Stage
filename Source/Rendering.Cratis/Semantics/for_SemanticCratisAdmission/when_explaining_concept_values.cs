// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_explaining_concept_values : Specification
{
    static ExecutableSemanticModel Model() => invoice_model.Compile("concept Display : String\n  validate\n    not empty message \"Required\"\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource));

    [Fact]
    void should_keep_values_only_concepts_renderable_as_enums()
    {
        var model = Model();
        var concept = model.Application.Concepts.Single() with { Values = ["First", "Second"], Validations = [] };
        var plan = invoice_model.Plan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Concepts = [concept] }));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        Assert.Empty(plan.Diagnostics);
        Assert.Contains(plan.Artifacts, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("enum Display", StringComparison.Ordinal));
    }

    [Fact]
    void should_explain_why_enumerations_cannot_carry_validation()
    {
        var model = Model();
        var concept = model.Application.Concepts.Single() with { Values = ["First", "Second"] };
        var plan = invoice_model.Plan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with { Concepts = [concept] }));
        var rule = concept.Validations[0].Name ?? concept.Validations[0].Kind.ToString();
        Assert.Equal($"Concept 'Display' combines enumerated values with validation '{rule}'; Stage renders enumerated values as a C# enum, which carries no concept validator, so these rules would be lost (Cratis/Stage#514).", Assert.Single(plan.Diagnostics).Message);
        Assert.Equal(concept.Id, plan.Diagnostics[0].Artifact);
        Assert.Equal("STAGE-ESM-002", plan.Diagnostics[0].Code);
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    void should_keep_validation_only_concepts_renderable()
    {
        var plan = invoice_model.Plan(Model());
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        Assert.Empty(plan.Diagnostics);
        Assert.Contains(plan.Artifacts, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("ConceptValidator<", StringComparison.Ordinal));
    }
}
