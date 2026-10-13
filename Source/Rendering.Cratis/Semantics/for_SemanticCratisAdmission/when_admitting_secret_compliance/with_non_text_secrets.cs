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

    [Fact] void should_admit_each_primitive_in_application_and_slice_scopes() => _plans.TrueForAll(entry => entry.Plan.Success).ShouldBeTrue();
    [Fact] void should_report_no_compliance_refusals() => _plans.SelectMany(entry => entry.Plan.Diagnostics).ShouldBeEmpty();
    [Fact] void should_preserve_encryption_and_audit_exclusion() => _plans.TrueForAll(entry => entry.Plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("Protected.cs", StringComparison.Ordinal) && System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("[global::Cratis.Chronicle.ProtectedValues.EncryptedAttribute", StringComparison.Ordinal) && System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("[global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute]", StringComparison.Ordinal))).ShouldBeTrue();
}
