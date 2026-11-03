// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation.for_PureValidationAdmission;

public class when_analysis_references_are_unavailable : Specification
{
    PureTransitionAdmission.Verdict _verdict = null!;
    Exception? _exception;

    void Because()
    {
        const string body = "return context.Value.Value > 0;";
        var loaded = validation_body_model.Load(validation_body_model.Concept(body));
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, CratisRendering.CreateProfile(loaded.Model.Application.Name, options), new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var previous = PureTransitionAdmission.ReferenceDirectoryOverride.Value;
        PureTransitionAdmission.ReferenceDirectoryOverride.Value = Path.Combine(Path.GetTempPath(), $"absent-stage-ref-{Guid.NewGuid():N}");
        try
        {
            _exception = Catch.Exception(() => _verdict = PureValidationAdmission.Analyze(body, context, loaded.TypedContextDescriptors.Single(), loaded.ImplementationRequirements.Single()));
        }
        finally
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = previous;
        }
    }

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_refuse_with_the_integrity_code() => _verdict.Code.ShouldEqual("STAGE-ESM-021");
    [Fact] void should_preserve_the_reference_failure_reason() => _verdict.Reason.ShouldContain("Reference assemblies 10.0.12");
}
#endif
