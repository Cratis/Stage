// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_concept_rule_predicate : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  validate\n    not empty\n" + Source).Replace("description String", "description Display", StringComparison.Ordinal));

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var concept = _model.Application.Concepts.Single();

        // Exercise the renderer boundary with a rule the executable-model compiler would reject.
        typeof(SemanticConcept).GetField("<Validations>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(concept, ImmutableArray.Create(concept.Validations.Single() with { Kind = SemanticValidationRuleKind.RulePredicate, RequirementId = "display-rule" }));
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        var request = new ArtifactRenderRequest(_model, plan, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [context.Slice(_slice.Id)]);
    }

    [Fact] void should_not_report_a_command_capability_diagnostic() => _diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _command.Id).ShouldBeEmpty();
    [Fact] void should_keep_the_concept_implementation_diagnostic() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Artifact == _model.Application.Concepts.Single().Id);
}
