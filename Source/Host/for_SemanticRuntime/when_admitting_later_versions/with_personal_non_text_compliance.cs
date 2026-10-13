// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_personal_non_text_compliance : Specification
{
    readonly List<SemanticExecutionPlan> _plans = [];
    readonly List<SemanticRuntimeAdmission> _admissions = [];

    void Establish()
    {
        foreach (var primitive in new[] { "Int", "Decimal", "Bool", "Date", "DateTime", "Uuid" })
        {
            foreach (var classification in new[] { "pii", "pii secret", string.Empty })
            {
                _plans.Add(compiled_plan.From($"concept Protected : {primitive} {classification}\n" + """
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

    void Because() => _admissions.AddRange(_plans.Select(plan => new SemanticRuntimeAdmission(plan)));

    [Fact] void should_admit_pii_personal_secrets_and_unmarked_non_text_values() => _admissions.SelectMany(admission => admission.Blocking).ShouldBeEmpty();
}
