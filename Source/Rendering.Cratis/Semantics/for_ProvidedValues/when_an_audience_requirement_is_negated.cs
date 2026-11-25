// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_an_audience_requirement_is_negated : Specification
{
    ImmutableArray<ArtifactRenderDiagnostic> _direct;
    ImmutableArray<ArtifactRenderDiagnostic> _nested;
    ArtifactRenderPlan _positive = null!;

    void Because()
    {
        _direct = Admit(nested: false);
        _nested = Admit(nested: true);
        _positive = a_fleet_model.Plan(an_available_fleet_model.Source);
    }

    static ImmutableArray<ArtifactRenderDiagnostic> Admit(bool nested)
    {
        var source = nested
            ? an_available_fleet_model.Source.Replace("  for Plus\n", "  for Plus or Beta\n", StringComparison.Ordinal)
            : an_available_fleet_model.Source;
        var compilation = a_fleet_model.Compile(source);
        var application = compilation.Model.Application;
        var availability = application.Availabilities.Single();
        var audience = new SemanticAudienceRequirement("Plus") { Negated = true };
        SemanticAvailabilityRequirement requirement = nested
            ? ((SemanticLogicalAvailability)availability.Requirements[0]) with { Left = audience }
            : audience;
        availability = availability with { Requirements = [requirement, .. availability.Requirements.Skip(1)] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V58, SemanticVersion.V58, application with { Availabilities = [availability] });
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model, []).Plan!,
            CratisRendering.CreateProfile("Fleet", new("Fleet", "Fleet")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Fleet", "Fleet"));
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticAvailabilityRendering.Admit(context, diagnostics);
        return [.. diagnostics];
    }

    [Fact] void should_refuse_the_requirement() => _direct.Any(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_explain_the_unsupported_negation() => Assert.True(_direct.Any(diagnostic => diagnostic.Code == SemanticAvailabilityRendering.UnsupportedCondition && diagnostic.Message.Contains("negated audience 'Plus'", StringComparison.Ordinal)), string.Join("; ", _direct));
    [Fact] void should_also_refuse_negation_inside_a_logical_requirement() => _nested.Any(diagnostic => diagnostic.Code == SemanticAvailabilityRendering.UnsupportedCondition).ShouldBeTrue();
    [Fact] void should_keep_non_negated_audiences_renderable() => Assert.True(_positive.Success, string.Join("; ", _positive.Diagnostics));
}
#endif
