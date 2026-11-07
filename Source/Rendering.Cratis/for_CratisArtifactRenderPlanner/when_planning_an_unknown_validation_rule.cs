// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_an_unknown_validation_rule : given.a_register_project_render_request
{
    [Fact]
    void should_refuse_the_owner_before_emitting_any_artifacts()
    {
        var command = _registerProject.Commands.Single();
        var rule = command.Validations[0] with { Kind = (SemanticValidationRuleKind)999 };
        var concept = _model.Application.Concepts[0];
        var application = _model.Application with { Concepts = [concept with { Validations = [rule] }, .. _model.Application.Concepts.Skip(1)] };

        // Bypass Screenplay's constructor validation to model an unaudited dependency enum value.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_model, application);
        var plan = _planner.Plan(_request);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Artifact == concept.Id && diagnostic.Message.Contains("999", StringComparison.Ordinal) && diagnostic.Message.Contains(concept.Name, StringComparison.Ordinal));
    }
}
