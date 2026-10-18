// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_event_concepts_with_unevaluable_rules : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRun[] _reference = [];

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("HistoricalFacts"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("facts"), "facts", "Facts.play", Source);
        var compilation = new SemanticModelCompiler().Compile("HistoricalFacts", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    async Task Because()
    {
        _reference = [.. _plan.Specifications.Keys.Select(id => new SemanticSpecificationRunner().Run(_plan, id))];
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
    }

    [Fact] void should_exercise_a_rule_stage_cannot_evaluate() => SemanticValidationRules.Check(_plan.Model.Application.Concepts.Single().Validations.Single(), SemanticValue.Null).Status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
    [Fact] void should_admit_given_appended_and_produced_event_shapes() => Assert.All(_plan.Specifications.Values, specification => Assert.Null(SemanticRunAdmission.Check(_plan, specification)));
    [Fact] void should_preserve_reference_execution() => Assert.All(_reference, result => Assert.True(result.Passed, string.Join(';', result.Failures)));
    [Fact] void should_execute_all_four_specs() => _report.Results.Count.ShouldEqual(4);
    [Fact] void should_not_apply_command_rules_to_facts() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}"));

    const string Source = """
        concept CheckedLabel : String
          validate
            rule ApprovedLabel
              ```csharp
              return true;
              ```
        type Details
          label CheckedLabel
        module History
          feature Recording
            slice StateChange Record
              command Echo
                id String
                returns id
              command Record
                id String identifier
                value String
                produces Recorded
                  for id
                  label = "x"
              command RecordComposite
                id String identifier
                details Details
                produces CompositeRecorded
                  for id
                  details = details
              event Recorded
                label CheckedLabel
              event CompositeRecorded
                details Details
              specification GivenConceptFact
                given Recorded
                  for "before"
                  label = "x"
                when Echo
                  id = "one"
                then returns "one"
              specification AppendedConceptFact
                when append Recorded
                  for "one"
                  label = "x"
                then no events
              specification ProducedConceptFact
                when Record
                  id = "one"
                  value = "x"
                then Recorded
                  label = "x"
              specification AppendedCompositeConceptFact
                when append CompositeRecorded
                  for "one"
                  details = {"label":"x"}
                then no events
        """;
}
