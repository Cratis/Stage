// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_secret_compliance;

public class with_non_text_secrets : Specification
{
    readonly List<ExecutableSemanticModel> _models = [];
    readonly List<(ArtifactRenderPlan Plan, SemanticId Concept)> _plans = [];

    void Establish()
    {
        foreach (var primitive in new[] { "Int", "Decimal", "Bool", "Date", "DateTime", "Uuid" })
        {
            foreach (var scope in new[] { string.Empty, " scope subject", " scope namespace", " scope global" })
            {
                var declaration = $"concept Protected : {primitive} secret\n";
                if (scope.Length > 0) declaration += $"  secret{scope}\n";
                _models.Add(invoice_model.Compile(declaration + """
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

    void Because()
    {
        foreach (var model in _models)
        {
            var concept = model.Application.Concepts.Single().Id;
            _plans.Add((invoice_model.Plan(model), concept));
            var slice = SemanticVersionFeatures.Slices(model.Application).Single();
            _plans.Add((invoice_model.Plan(model, new(ArtifactRenderScopeKind.Slice, slice.Id)), concept));
        }
    }

    [Fact] void should_refuse_each_primitive_in_application_and_slice_scopes() => _plans.TrueForAll(entry => entry.Plan.Diagnostics.Length == 1 && entry.Plan.Diagnostics[0].Code == "STAGE-ESM-041").ShouldBeTrue();
    [Fact] void should_name_the_secret_concept_as_the_artifact() => _plans.TrueForAll(entry => entry.Plan.Diagnostics[0].Artifact == entry.Concept).ShouldBeTrue();
    [Fact] void should_emit_no_artifacts() => _plans.SelectMany(entry => entry.Plan.Artifacts).ShouldBeEmpty();
}
