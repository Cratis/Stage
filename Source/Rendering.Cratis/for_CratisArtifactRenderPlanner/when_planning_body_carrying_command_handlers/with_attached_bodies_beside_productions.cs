// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;

/// <summary>
/// Screenplay#1068 admits a command handler that carries an attached or inline body beside <c language="text">produces</c>. Until Stage
/// splices an authenticated body (Stage#808) such a command must be refused with a typed diagnostic, never crash, drop the
/// body silently, or render a partial command. The model is the V23 handler-presence vector with its pending requirement
/// resolved to a body, which is the only difference between a described and a body-carrying handler in the ESM.
/// </summary>
public class with_attached_bodies_beside_productions : Specification
{
    static readonly (string Name, string Language, string? File, string Body)[] _scenarios =
    [
        ("inline context-independent csharp", "csharp", null, "return new object[] { 42 };"),
        ("inline context-binding csharp", "csharp", null, "return new object[] { context.Identity.Id };"),
        ("file-backed csharp", "csharp", "Handlers/ApplyLateFee.cs", "return new object[] { 42 };"),
        ("inline python", "python", null, "return [42]"),
        ("file-backed python", "python", "Handlers/apply_late_fee.py", "return [42]"),
    ];

    ExecutableSemanticModel _model = null!;
    string _requirementId = null!;
    Dictionary<string, ArtifactRenderPlan> _plans = null!;
    Dictionary<string, ArtifactRenderPlan> _stubPlans = null!;
    Dictionary<string, Exception?> _errors = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(DescribedCommandHandlersCorpus.V23.EsmBytes.AsSpan());
        _requirementId = _model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.Commands).Single(command => command.Handler is not null).Handler!.RequirementId;
        _plans = [];
        _stubPlans = [];
        _errors = [];
    }

    void Because()
    {
        var pending = DescribedRequirement();
        foreach (var (name, language, file, body) in _scenarios)
        {
            var resolved = pending with
            {
                AttachmentResolution = SemanticAttachmentResolution.Resolved,
                Language = language,
                File = file,
                ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant(),
                IsProvisional = false
            };
            try
            {
                _plans[name] = Plan(resolved, body, ImplementationGapMode.Refuse);
                _stubPlans[name] = Plan(resolved, body, ImplementationGapMode.Stub);
            }
            catch (Exception exception)
            {
                _errors[name] = exception;
            }
        }
    }

    [Fact] void should_not_throw_for_any_body() => _errors.ShouldBeEmpty();
    [Fact] void should_plan_every_scenario() => _plans.Count.ShouldEqual(_scenarios.Length);
    [Fact] void should_refuse_every_body_with_the_handler_code_and_identity() => _plans.Values.All(plan => plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-IMPL-002" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains(_requirementId, StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_report_a_refused_outcome() => _plans.Values.All(plan => plan.Outcome == ArtifactRenderOutcome.Refused).ShouldBeTrue();
    [Fact] void should_not_report_the_body_as_unresolved() => _plans.Values.All(plan => !plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-020")).ShouldBeTrue();
    [Fact] void should_emit_no_partial_artifacts() => _plans.Values.All(plan => plan.Artifacts.IsEmpty).ShouldBeTrue();
    [Fact] void should_not_render_the_body_anywhere() => _plans.Values.All(plan => plan.ImplementationGaps.IsEmpty).ShouldBeTrue();
    [Fact] void should_not_turn_into_a_stub_when_stubs_are_enabled() => _stubPlans.Values.All(plan => plan.Artifacts.IsEmpty).ShouldBeTrue();
    [Fact] void should_still_refuse_with_an_error_naming_the_requirement_when_stubs_are_enabled() => _stubPlans.Values.All(plan => plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-IMPL-002" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains(_requirementId, StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_report_a_refused_outcome_when_stubs_are_enabled() => _stubPlans.Values.All(plan => plan.Outcome == ArtifactRenderOutcome.Refused).ShouldBeTrue();
    [Fact] void should_not_report_the_body_as_unresolved_when_stubs_are_enabled() => _stubPlans.Values.All(plan => !plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-020")).ShouldBeTrue();
    [Fact] void should_leave_no_gap_when_stubs_are_enabled() => _stubPlans.Values.All(plan => plan.ImplementationGaps.IsEmpty).ShouldBeTrue();

    ArtifactRenderPlan Plan(SemanticImplementationRequirement requirement, string body, ImplementationGapMode mode) =>
        CratisRendering.Plan(
            _model,
            SemanticExecutionPlan.Compile(_model, []).Plan!,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id),
            new("Described", "Described") { ImplementationGaps = mode },
            [requirement],
            ImmutableDictionary<string, string>.Empty.Add(_requirementId, body));

    SemanticImplementationRequirement DescribedRequirement()
    {
        var vector = DescribedCommandHandlersCorpus.V23;
        var form = vector.SourceForms.Single(source => source.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(vector.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation.ImplementationRequirements.Single();
    }
}
