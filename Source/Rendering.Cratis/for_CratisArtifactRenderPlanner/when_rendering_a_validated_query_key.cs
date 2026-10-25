// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_a_validated_query_key : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _query = null!;
    string _common = null!;

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(a_validated_query_key_model.Source));
        _query = Content("Lookup.cs");
        _common = Content("InvoiceId.cs");
    }

    [Fact] void should_admit_the_query() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_concept_typed_query_parameters() => _query.ShouldContain("IReadModels readModels, InvoiceId invoiceId)");
    [Fact] void should_use_the_validated_lookup_key() => _query.ShouldContain("GetInstanceById<InvoiceSummary>((EventSourceId)invoiceId)");
    [Fact] void should_compare_concepts_inside_the_collection_filter() => _query.ShouldContain("EqualityComparer<InvoiceId>.Default.Equals(instance.InvoiceId, invoiceId)");
    [Fact] void should_still_render_the_concept_validator() => _common.ShouldContain("ConceptValidator<InvoiceId>");
    [Fact] void should_keep_commands_concept_typed() => Content("Issue.cs").ShouldContain("InvoiceId InvoiceId");

    string Content(string name) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
