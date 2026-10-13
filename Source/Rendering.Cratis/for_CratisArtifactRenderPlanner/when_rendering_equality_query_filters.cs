// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_equality_query_filters : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _query = null!;
    string _specification = null!;

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(a_query_filter_model.Source + "\n" + a_query_filter_model.Specification));
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        _query = Content("Lookup.cs");
        _specification = Content("when_filtering_invoices_is_queried.cs");
    }

    [Fact] void should_admit_equality_filters() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_declaration_order_and_optional_defaults() => _query.ShouldContain("IReadModels readModels, string status, string? category = null)");
    [Fact] void should_combine_the_key_with_filters() => _query.ShouldContain("IReadModels readModels, global::Invoices.Common.InvoiceId invoiceId, string status, string? category = null)");
    [Fact] void should_guard_missing_required_text_filters() => _query.ShouldContain("MissingArgumentForQuery(nameof(status), typeof(string)");
    [Fact] void should_skip_omitted_optional_filters() => _query.ShouldContain("category == null || global::System.Collections.Generic.EqualityComparer<global::Invoices.Common.Category?>.Default.Equals");
    [Fact] void should_convert_validated_filter_concepts_inside_the_method() => _query.ShouldContain("new global::Invoices.Common.Category(category)");
    [Fact] void should_pass_named_filter_values_to_companion_specs() => _specification.ShouldContain("status: \"open\", category: \"North\"");
    [Fact] void should_return_arc_observable_collections() => _query.ShouldContain("ISubject<global::System.Collections.Generic.IEnumerable<InvoiceSummary>>");
    [Fact] void should_observe_the_filtered_read_model() => _query.ShouldContain("readModels.Observe<InvoiceSummary>(instance =>");
    [Fact] void should_execute_the_real_observation_in_companion_specs() => _specification.ShouldContain("EventStoreForTesting _scenario = new()");
    [Fact] void should_await_the_first_emission_in_companion_specs() => _specification.ShouldContain("Observable.FirstAsync(");
    [Fact] void should_count_filtered_rows_in_companion_specs() => _specification.ShouldContain("Enumerable.Count(_results) == 1");

    string Content(string name) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
