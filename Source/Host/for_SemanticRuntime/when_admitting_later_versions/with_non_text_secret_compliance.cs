// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_non_text_secret_compliance : Specification
{
    readonly List<SemanticExecutionPlan> _plans = [];
    readonly List<(SemanticRuntimeAdmission Admission, string Concept)> _admissions = [];

    void Establish()
    {
        foreach (var primitive in new[] { "Int", "Decimal", "Bool", "Date", "DateTime", "Uuid" })
        {
            foreach (var scope in new[] { string.Empty, " scope subject", " scope namespace", " scope global" })
            {
                var declaration = $"concept Protected : {primitive} secret\n";
                if (scope.Length > 0) declaration += $"  secret{scope}\n";
                _plans.Add(compiled_plan.From(declaration + """
                    module Records
                      feature Registration
                        slice StateChange Register
                          command Register
                            id String identifier
                            value Protected
                            produces Registered
                              for id
                              value = value
                          event Registered
                            value Protected
                    """));
            }
        }
    }

    void Because() => _admissions.AddRange(_plans.Select(plan => (new SemanticRuntimeAdmission(plan), plan.Model.Application.Concepts.Single().Id.ToString())));

    [Fact] void should_block_non_text_secrets_before_runtime_execution() => _admissions.TrueForAll(entry => entry.Admission.Blocking.Count == 1 && entry.Admission.Blocking[0].Details!.StartsWith("STAGE-ESM-041:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_the_secret_concept() => _admissions.TrueForAll(entry => entry.Admission.Blocking[0].Artifact == entry.Concept && entry.Admission.Blocking[0].Kind == "concept").ShouldBeTrue();
    [Fact] void should_report_an_occurrence_capability_refusal() => _admissions.TrueForAll(entry => entry.Admission.Blocking[0].Capability == "Occurrence").ShouldBeTrue();
}
