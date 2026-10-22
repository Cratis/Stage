// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_queries;

public class with_an_observable_optional_lookup_rendering_an_expression_tree : Specification
{
    ArtifactRenderPlan[] _plans = [];
    RenderedFile[][] _files = [];
    string _source = string.Empty;

    void Because()
    {
        var source = a_validated_query_key_model.Source.Replace("query InvoiceById => InvoiceSummary?", "query InvoiceById => observable InvoiceSummary?", StringComparison.Ordinal);
        var primitive = source.Replace("concept InvoiceId : String\n  validate\n    min 2\n", string.Empty, StringComparison.Ordinal)
            .Replace("invoiceId InvoiceId", "invoiceId String", StringComparison.Ordinal);
        _plans = [invoice_model.Plan(invoice_model.Compile(source)), invoice_model.Plan(invoice_model.Compile(primitive))];
        _files = [.. _plans.Select(plan => plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray())];
        _source = _files[0].First(file => file.Content.Contains("ISubject<InvoiceSummary?> InvoiceById", StringComparison.Ordinal)).Content;
    }

    [Fact] void should_admit_concept_and_primitive_identifier_keys() => _plans.SelectMany(plan => plan.Diagnostics).ShouldBeEmpty();
    [Fact] void should_render_a_nullable_single_instance_subject() => _source.ShouldContain("ISubject<InvoiceSummary?> InvoiceById");
    [Fact] void should_render_the_caller_supplied_key_parameter() => _source.ShouldContain("InvoiceId invoiceId)");
    [Fact] void should_observe_chronicle_with_a_predicate() => _source.ShouldContain("readModels.Observe<InvoiceSummary>(instance => ");
    [Fact] void should_filter_chronicle_observation_by_the_identifier() => _source.ShouldContain(".Default.Equals(instance.InvoiceId, invoiceId)");
    [Fact] void should_select_the_current_instance_or_null() => _source.ShouldContain("Observable.Select(instances, values => global::System.Linq.Enumerable.FirstOrDefault(values))");
    [Fact] void should_compile_concept_key_expressions() => RenderedOutput.Errors(_files[0]).ShouldBeEmpty();
    [Fact] void should_compile_primitive_key_expressions() => RenderedOutput.Errors(_files[1]).ShouldBeEmpty();
}
