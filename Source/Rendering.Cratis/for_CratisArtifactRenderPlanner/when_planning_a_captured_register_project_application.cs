// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_captured_register_project_application : a_register_project_render_request
{
    readonly List<SemanticCompilation> _compilations = [];
    readonly List<ExecutableSemanticModel> _capturedModels = [];
    readonly List<byte[]> _captures = [];
    readonly List<byte[]> _reserializedCaptures = [];
    readonly List<SemanticExecutionPlanCompilation> _capturedExecutions = [];
    readonly List<SemanticExecutionPlanCompilation> _directExecutions = [];
    readonly List<ArtifactRenderPlan> _capturedPlans = [];
    readonly List<ArtifactRenderPlan> _directPlans = [];
    readonly List<string> _calculatedHashes = [];

    void Because()
    {
        foreach (var form in Corpus.SourceForms)
        {
            var compilation = Compile(form);
            _compilations.Add(compilation);
            var capture = SemanticModelSerializer.Serialize(compilation.Model);
            _captures.Add(capture);
            var capturedModel = SemanticModelSerializer.Deserialize(capture);
            _capturedModels.Add(capturedModel);
            _reserializedCaptures.Add(SemanticModelSerializer.Serialize(capturedModel));
            var capturedExecution = SemanticExecutionPlan.Compile(capturedModel);
            _capturedExecutions.Add(capturedExecution);
            var capturedScope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, capturedModel.Application.Id);
            var capturedPlan = CratisRendering.Plan(capturedModel, capturedExecution.Plan!, capturedScope, _options);
            _capturedPlans.Add(capturedPlan);

            // The direct route is only a comparison: neither its model nor its execution plan feeds the captured route.
            var directExecution = SemanticExecutionPlan.Compile(compilation.Model);
            _directExecutions.Add(directExecution);
            var directScope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id);
            _directPlans.Add(CratisRendering.Plan(compilation.Model, directExecution.Plan!, directScope, _options));
        }

        _calculatedHashes.AddRange(_capturedPlans.Concat(_directPlans).SelectMany(_ => _.Artifacts)
            .Select(_ => Convert.ToHexString(SHA256.HashData(_.Bytes.AsSpan())).ToLowerInvariant()));
    }

    [Fact] void should_capture_both_canonical_physical_forms() => Corpus.SourceForms.Select(_ => _.Name).ShouldEqual("single", "folder");
    [Fact] void should_admit_both_semantic_compilations() => _compilations.Count.ShouldEqual(2);
    [Fact] void should_keep_the_canonical_corpus_identity() => Corpus.Name.ShouldEqual("register-project/v1-legacy");
    [Fact] void should_keep_the_corpus_application_identity_in_both_catalogs() => _compilations.TrueForAll(_ => _.Documents.IdentityCatalog.Application == Corpus.ApplicationIdentity).ShouldBeTrue();
    [Fact] void should_preserve_the_catalog_assigned_application_identity() => _capturedModels.Zip(_compilations).All(pair => pair.First.Application.Id == pair.Second.Documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForApplication(Corpus.ApplicationIdentity))).ShouldBeTrue();
    [Fact] void should_keep_the_expected_captured_revision() => _capturedModels.TrueForAll(_ => _.Revision == Corpus.SemanticRevision).ShouldBeTrue();
    [Fact] void should_capture_the_exact_canonical_corpus_bytes() => _captures.TrueForAll(_ => _.SequenceEqual(Corpus.EsmBytes)).ShouldBeTrue();
    [Fact] void should_reserialize_each_capture_byte_for_byte() => _captures.Zip(_reserializedCaptures).All(pair => pair.First.SequenceEqual(pair.Second)).ShouldBeTrue();
    [Fact] void should_admit_both_captured_execution_plans() => _capturedExecutions.TrueForAll(_ => _.Success).ShouldBeTrue();
    [Fact] void should_derive_execution_only_from_the_deserialized_models() => _capturedExecutions.Zip(_capturedModels).All(pair => ReferenceEquals(pair.First.Plan!.Model, pair.Second)).ShouldBeTrue();
    [Fact] void should_admit_both_direct_execution_plans() => _directExecutions.TrueForAll(_ => _.Success).ShouldBeTrue();
    [Fact] void should_admit_all_application_render_plans() => _capturedPlans.Concat(_directPlans).All(_ => _.Success).ShouldBeTrue();
    [Fact] void should_plan_nonempty_applications() => _capturedPlans.Concat(_directPlans).All(_ => !_.Artifacts.IsEmpty).ShouldBeTrue();
    [Fact] void should_include_the_application_project() => _capturedPlans.Concat(_directPlans).All(_ => _.Artifacts.Any(artifact => artifact.RelativePath == "Projects.csproj")).ShouldBeTrue();
    [Fact] void should_render_the_captured_semantic_revision() => _capturedPlans.TrueForAll(_ => _.SemanticRevision == Corpus.SemanticRevision).ShouldBeTrue();
    [Fact] void should_order_all_paths_ordinally() => _capturedPlans.Concat(_directPlans).All(_ => _.Artifacts.Select(artifact => artifact.RelativePath).SequenceEqual(_.Artifacts.Select(artifact => artifact.RelativePath).Order(StringComparer.Ordinal))).ShouldBeTrue();
    [Fact] void should_match_each_direct_plans_ordered_paths() => _capturedPlans.Zip(_directPlans).All(pair => pair.First.Artifacts.Select(_ => _.RelativePath).SequenceEqual(pair.Second.Artifacts.Select(_ => _.RelativePath))).ShouldBeTrue();
    [Fact] void should_match_each_direct_plans_raw_bytes() => Assert.True(
        _capturedPlans.Zip(_directPlans).All(pair => pair.First.Artifacts.Zip(pair.Second.Artifacts).All(artifacts => artifacts.First.Bytes.SequenceEqual(artifacts.Second.Bytes))),
        DescribeArtifactMismatches());
    [Fact] void should_match_each_direct_plans_hashes() => Assert.True(
        _capturedPlans.Zip(_directPlans).All(pair => pair.First.Artifacts.Select(_ => _.Sha256).SequenceEqual(pair.Second.Artifacts.Select(_ => _.Sha256))),
        DescribeArtifactMismatches());
    [Fact] void should_match_ordered_paths_across_captured_physical_forms() => _capturedPlans[0].Artifacts.Select(_ => _.RelativePath).SequenceEqual(_capturedPlans[1].Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_match_raw_bytes_across_captured_physical_forms() => _capturedPlans[0].Artifacts.Zip(_capturedPlans[1].Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_match_hashes_across_captured_physical_forms() => _capturedPlans[0].Artifacts.Select(_ => _.Sha256).SequenceEqual(_capturedPlans[1].Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_hash_the_actual_emitted_bytes() => _calculatedHashes.SequenceEqual(_capturedPlans.Concat(_directPlans).SelectMany(_ => _.Artifacts).Select(_ => _.Sha256)).ShouldBeTrue();

    string DescribeArtifactMismatches() => string.Join(Environment.NewLine, _capturedPlans.Zip(_directPlans).Select((pair, index) =>
        $"Physical form: {Corpus.SourceForms[index].Name}; captured artifacts: {pair.First.Artifacts.Length}; direct artifacts: {pair.Second.Artifacts.Length}{Environment.NewLine}" +
        string.Join(Environment.NewLine, pair.First.Artifacts.Zip(pair.Second.Artifacts)
            .Where(artifacts => !artifacts.First.Bytes.SequenceEqual(artifacts.Second.Bytes) || artifacts.First.Sha256 != artifacts.Second.Sha256)
            .Select(artifacts =>
                $"Captured: {artifacts.First.RelativePath} ({artifacts.First.Bytes.Length} bytes; SHA256 {artifacts.First.Sha256}){Environment.NewLine}" +
                $"Direct: {artifacts.Second.RelativePath} ({artifacts.Second.Bytes.Length} bytes; SHA256 {artifacts.Second.Sha256}){Environment.NewLine}" +
                $"--- direct source ---{Environment.NewLine}{Text(artifacts.Second)}{Environment.NewLine}" +
                $"--- captured source ---{Environment.NewLine}{Text(artifacts.First)}"))));
}
