// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_admitting_code_provided_values : Specification
{
    [Theory]
    [InlineData("String", "return context.Tenant.Value;")]
    [InlineData("String", "return context.Identity.Id;")]
    [InlineData("String", "return context.Occurred.ToString();")]
    [InlineData("String", "return context.Arguments.Name;")]
    [InlineData("Bool", "return true;")]
    [InlineData("Int", "return 42;")]
    [InlineData("Uuid", "return global::System.Guid.Empty;")]
    [InlineData("Vessel optional", "return null;")]
    [InlineData("Vessel", "return new global::Fleet.Fleet.Vessels.Vessels.Vessel(Name: \"Aurora\", VesselId: global::System.Guid.Empty);")]
    void should_admit_a_supported_body(string type, string body) => a_code_provided_model.AssertSuccess(a_code_provided_model.Plan(body, type));

    [Theory]
    [InlineData("return true;")]
    [InlineData("return context.Clock.ToString();")]
    [InlineData("not valid C#")]
    [InlineData("}\npublic class Escaped {}\n{")]
    void should_refuse_invalid_or_escaping_bodies_before_emitting_artifacts(string body)
    {
        var plan = a_code_provided_model.Plan(body);
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-075").ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_tampered_body()
    {
        var request = a_code_provided_model.Request("return \"Nordics\";");
        var id = request.ImplementationRequirements.Single().RequirementId;
        var plan = a_code_provided_model.Plan(request with { ImplementationContents = request.ImplementationContents.SetItem(id, "return \"tampered\";") });
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == SemanticImplementationAdmission.BodyIntegrityCode).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_missing_contents()
    {
        var request = a_code_provided_model.Request("return \"Nordics\";");
        a_code_provided_model.Plan(request with { ImplementationContents = [] }).Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData(SemanticAttachmentResolution.Pending)]
    [InlineData(SemanticAttachmentResolution.UnresolvedFile)]
    void should_refuse_an_unresolved_attachment(SemanticAttachmentResolution resolution)
    {
        var request = a_code_provided_model.Request("return \"Nordics\";");
        var requirement = request.ImplementationRequirements.Single() with { AttachmentResolution = resolution };
        a_code_provided_model.Plan(request with { ImplementationRequirements = [requirement] }).Success.ShouldBeFalse();
    }
}
#endif
