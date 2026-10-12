// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_secret_containing_inputs : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _command = string.Empty;

    void Because()
    {
        var source = a_concept_compliance_model.ContainerTypes + "\n" + a_concept_compliance_model.Source + "\n" + a_concept_compliance_model.ContainerSlice;
        _plan = invoice_model.Plan(invoice_model.Compile(source));
        _command = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/RecordCredentials.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_admit_the_complete_application() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_exclude_secret_collection_inputs() => _command.ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] global::System.Collections.Generic.IReadOnlyList<global::Invoices.Common.ApiKey> Keys");
    [Fact] void should_exclude_composites_with_secrets_in_nested_collections() => _command.ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] global::Invoices.Common.CredentialEnvelope Credentials");
    [Fact] void should_exclude_optional_composites() => _command.ShouldContain("[property: global::Cratis.Arc.Chronicle.Commands.NotAuditedAttribute] global::Invoices.Common.CredentialEnvelope? OptionalCredentials");
    [Fact] void should_keep_unrelated_inputs_auditable() => _command.ShouldContain(", string Visible)");
}
