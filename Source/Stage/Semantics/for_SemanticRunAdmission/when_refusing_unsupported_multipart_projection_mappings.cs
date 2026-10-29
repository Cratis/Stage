// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_refusing_unsupported_multipart_projection_mappings : Specification
{
    string? _nested;
    string? _clear;
    string? _optionalProduct;
    string? _supportedProduct;
    string? _mappingFree;

    void Because()
    {
        _nested = Rejection("details.amount = amount", "Details optional");
        _clear = Rejection("clear amount", "Details optional");
        _optionalProduct = Rejection("add amount by amount * amount", "Details optional", optionalFactor: true);
        _supportedProduct = Rejection("add amount by amount * amount", "Details optional");
        _mappingFree = Rejection("", "Details optional");
    }

    [Fact] void should_refuse_nested_targets_before_they_are_truncated() => _nested.ShouldEqual("This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.");
    [Fact] void should_refuse_clear_before_mapping_execution() => _clear.ShouldEqual("This admitted ESM scope needs a per-run lowering beyond scalar from/set mappings.");
    [Fact] void should_refuse_a_product_with_an_optional_factor_before_lowering() => _optionalProduct.ShouldEqual("A projection adds or subtracts a product of two event values (ESM v29) that cannot be carried on the event: 'amount' and 'amount' must both be required whole or decimal numbers.");
    [Fact] void should_keep_a_carriable_product_admitted() => _supportedProduct.ShouldBeNull();
    [Fact] void should_refuse_a_mapping_free_from_without_required_non_key_properties() => _mappingFree.ShouldEqual("A mapping-free from block on a read model without required properties does not materialize a Chronicle read model.");

    static string? Rejection(string mapping, string detailsType, bool optionalFactor = false)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MultipartMappings"));
        var source = Source.Replace("amount = amount", mapping, StringComparison.Ordinal)
            .Replace("details Details", $"details {detailsType}", StringComparison.Ordinal)
            .Replace("FACTOR_TYPE", optionalFactor ? "Int optional" : "Int", StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", source);
        var compilation = new SemanticModelCompiler().Compile("MultipartMappings", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        return SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())?.Details;
    }

    const string Source = """
        type Details
          amount Int
        module Billing
          feature Payments
            slice StateView Status
              event Paid
                tenant String
                invoice String
                amount FACTOR_TYPE
              readmodel Payment
                tenant String key
                invoice String key
                amount Int optional
                details Details
              projection Payments => Payment
                no automap
                from Paid
                  key
                    tenant = tenant
                    invoice = invoice
                  amount = amount
              specification Existing
                given Paid
                  tenant = "north"
                  invoice = "first"
                  amount = 1
                then readmodel Payment
                  tenant = "north"
                  invoice = "first"
        """;
}
