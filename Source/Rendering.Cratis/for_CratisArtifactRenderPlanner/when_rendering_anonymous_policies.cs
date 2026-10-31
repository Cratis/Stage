// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_anonymous_policies : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_admit_the_corpus() => _plan.Success.ShouldBeTrue();
    [Fact] void should_remain_publishable() => _plan.CanPublishArtifacts.ShouldBeTrue();
    [Fact] void should_not_emit_policy_artifacts() => _plan.Artifacts.ShouldNotContain(artifact => artifact.RelativePath.Contains("GeneratedPolicies", StringComparison.Ordinal) && !artifact.RelativePath.EndsWith("Registration.cs", StringComparison.Ordinal));

    [Fact]
    void should_explain_each_explicitly_anonymous_operation_once()
    {
        var operations = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Commands.Select(command => command.Id).Concat(slice.Queries.Select(query => query.Id))).ToArray();
        var diagnostics = _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-060").ToArray();
        diagnostics.Length.ShouldEqual(operations.Length);
        foreach (var operation in operations)
        {
            var diagnostic = diagnostics.Single(diagnostic => diagnostic.Artifact == operation);
            diagnostic.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Information);
            diagnostic.Message.ShouldContain("explicitly anonymous (require anonymous): any caller may invoke it");
        }
    }

    [Fact]
    void should_not_report_explicit_anonymous_access_for_missing_authorization()
    {
        foreach (var slice in _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices))
        {
            foreach (var command in slice.Commands) typeof(SemanticCommand).GetProperty(nameof(SemanticCommand.Authorization)).SetValue(command, null);
            foreach (var query in slice.Queries) typeof(SemanticKeyedQuery).GetProperty(nameof(SemanticKeyedQuery.Authorization)).SetValue(query, null);
        }
        invoice_model.Plan(_model).Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-060");
    }

    [Fact]
    void should_refuse_a_denial_expectation_on_an_anonymous_operation()
    {
        var specification = _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Specifications).First(specification => specification.When is not null);
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenDenied)).SetValue(specification, true);
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenEvents)).SetValue(specification, specification.ThenEvents.Clear());
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenReadModels)).SetValue(specification, specification.ThenReadModels.Clear());
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenQueries)).SetValue(specification, specification.ThenQueries.Clear());
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.GivenCaller)).SetValue(specification, new SemanticCaller(true, [], []));
        var plan = invoice_model.Plan(_model);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == specification.Id);
        plan.Artifacts.ShouldNotContain(artifact => artifact.Sources.Contains(specification.Id));
    }

    [Fact]
    void should_render_short_allow_anonymous_attributes()
    {
        var sources = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Where(source => source.Contains("[AllowAnonymous]", StringComparison.Ordinal)).ToArray();
        sources.ShouldNotBeEmpty();
        FrameworkNameCatalog.Current.GlobalUsings(GeneratedProjects.Application).ShouldContain("Cratis.Arc.Authorization");
        sources.ShouldEachConformTo(source => !source.Contains("[global::Cratis.Arc.Authorization.AllowAnonymous", StringComparison.Ordinal) &&
            !source.Contains("[Authorize", StringComparison.Ordinal));
    }
}
