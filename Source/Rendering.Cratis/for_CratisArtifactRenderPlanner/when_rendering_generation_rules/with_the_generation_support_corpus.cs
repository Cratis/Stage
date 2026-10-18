// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class with_the_generation_support_corpus : Specification
{
    readonly List<ArtifactRenderPlan> _plans = [];
    readonly List<(ArtifactRenderPlan Plan, string Body)> _bodies = [];

    void Because()
    {
        var corpus = GenerationSupportCorpus.V21;
        foreach (var form in corpus.SourceForms)
        {
            var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
            var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
                catalog.ResolveDocument(document.StableKey),
                document.StableKey,
                document.DisplayPath,
                document.Text)).ToArray();
            var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
            Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
            var model = compiled.Value!.Model;
            Assert.Equal(corpus.SemanticRevision, model.Revision);
            Assert.Equal([.. corpus.EsmBytes], SemanticModelSerializer.Serialize(model));
            var loaded = new LoadedSemanticModel(model, SemanticExecutionPlan.Compile(model).Plan!)
            {
                ImplementationRequirements = compiled.ImplementationRequirements,
                ImplementationContents = SemanticImplementationBodies.Resolve(compiled.Value.Documents, compiled.ImplementationRequirements),
                TypedContextDescriptors = compiled.TypedContextDescriptors
            };
            var plan = opaque_policy_model.Plan(loaded);
            _plans.Add(plan);
            var requirement = loaded.ImplementationRequirements.Single(requirement => requirement.Role == SemanticImplementationRole.ValueGeneration);
            _bodies.Add((plan, loaded.ImplementationContents[requirement.RequirementId]));
        }
    }

    [Fact] void should_admit_every_source_form_with_its_envelope() => _plans.ShouldEachConformTo(plan => plan.Success);
    [Fact] void should_render_all_four_source_forms() => _plans.Count.ShouldEqual(4);
    [Fact] void should_not_emit_a_factory_for_the_unused_rule_concept() => _plans.ShouldEachConformTo(plan => !Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Common/TenantOrderId.cs").Bytes.AsSpan()).Contains("Generate_", StringComparison.Ordinal));
    [Fact] void should_carry_a_non_empty_body() => _bodies.ShouldEachConformTo(item => Lines(item.Body).Any());
    [Fact] void should_compile_every_rendered_form() => _plans.ForEach(plan => RenderedOutput.Errors(opaque_policy_model.Files(plan)).ShouldBeEmpty());

    static IEnumerable<string> Lines(string body) => body.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
}
#endif
