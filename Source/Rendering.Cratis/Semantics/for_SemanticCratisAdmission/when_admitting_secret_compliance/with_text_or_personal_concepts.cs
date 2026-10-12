// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_secret_compliance;

public class with_text_or_personal_concepts : Specification
{
    readonly List<ExecutableSemanticModel> _models = [];
    readonly List<ArtifactRenderPlan> _plans = [];

    void Establish()
    {
        foreach (var primitive in new[] { "String", "Int", "Decimal", "Bool", "Date", "DateTime", "Uuid" })
        {
            foreach (var classification in primitive == "String" ? new[] { "secret", "pii", "pii secret", string.Empty } : ["pii", "pii secret", string.Empty])
            {
                _models.Add(invoice_model.Compile($"concept Protected : {primitive} {classification}\n" + """
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

    void Because() => _plans.AddRange(_models.Select(model => invoice_model.Plan(model)));

    [Fact] void should_preserve_text_secret_and_personal_precedence_admission() => _plans.SelectMany(plan => plan.Diagnostics).ShouldBeEmpty();
    [Fact] void should_emit_artifacts() => _plans.TrueForAll(plan => !plan.Artifacts.IsEmpty).ShouldBeTrue();
}
