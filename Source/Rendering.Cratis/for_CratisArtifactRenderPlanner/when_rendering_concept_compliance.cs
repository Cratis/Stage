// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_concept_compliance : Specification
{
    readonly List<ArtifactRenderPlan> _plans = [];
    readonly List<ExecutableSemanticModel> _models = [];

    void Establish() => _models.AddRange(ConceptComplianceCorpus.V13.SourceForms.Select(form => a_concept_compliance_model.Compile(form)));
    void Because() => _plans.AddRange(_models.Select(model => invoice_model.Plan(model)));

    [Fact] void should_admit_all_four_v13_forms() => _models.Select(model => model.SemanticVersion).ShouldContainOnly(SemanticVersion.V13, SemanticVersion.V13, SemanticVersion.V13, SemanticVersion.V13);
    [Fact] void should_emit_complete_plans() => _plans.SelectMany(plan => plan.Diagnostics).ShouldBeEmpty();
    [Fact] void should_render_personal_qualifiers_as_pii_only() => Concepts("HealthInformation").All(text => text.Contains("[global::Cratis.Chronicle.Compliance.GDPR.PIIAttribute]", StringComparison.Ordinal) && !text.Contains("ComplianceDetails", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_namespace_secret_protection() => Concepts("ApiKey").All(text => text.Contains("[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute(global::Cratis.Chronicle.ProtectedValues.EncryptionScope.Namespace)]", StringComparison.Ordinal) && text.Contains("[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_give_personal_classification_precedence() => Concepts("PersonalSecret").All(text => text.Contains("PIIAttribute", StringComparison.Ordinal) && !text.Contains("Encrypted", StringComparison.Ordinal) && !text.Contains("NotAudited", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_every_rendered_form() => _plans.SelectMany(plan => RenderedOutput.Errors(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs").Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))))).ShouldBeEmpty();

    IEnumerable<string> Concepts(string name) => _plans.Select(plan => Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs").Bytes.AsSpan()));
}
