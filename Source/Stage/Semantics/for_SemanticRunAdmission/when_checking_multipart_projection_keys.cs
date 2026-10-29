// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_checking_multipart_projection_keys : Specification
{
    string? _supported;
    string? _missing;
    string? _duplicate;
    string? _optional;
    string? _collection;
    string? _parent;
    string? _literal;
    string? _seed;
    string? _typedComposite;
    string? _mixedFrom;
    string? _mixedRemoval;
    string? _decimal;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MultipartAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("MultipartAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var model = plan.ReadModels.Values.Single();
        var spec = plan.Specifications.Values.Single();
        var scope = plan.Projections.Values.Single().Scope!;
        var projectionFrom = scope.From.Single();
        var parts = (SemanticProjectionPartsKey)projectionFrom.Key;
        string? Reject(SemanticProjectionKey key) => SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, model, scope with { From = [projectionFrom with { Key = key }] });

        _supported = SemanticRunAdmission.Check(plan, spec)?.Details;
        _missing = Reject(parts with { Parts = [parts.Parts[0]] });
        _duplicate = Reject(parts with { Parts = [parts.Parts[0], parts.Parts[0]] });
        _literal = Reject(parts with { Parts = [parts.Parts[0] with { Value = new SemanticProjectionLiteral(SemanticValue.Text("literal")) }, parts.Parts[1]] });
        _parent = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, model, scope with { From = [projectionFrom with { ParentKey = projectionFrom.Key }] });
        var property = model.Properties[0];
        _optional = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, model with { Properties = model.Properties.SetItem(0, property with { Type = property.Type with { IsOptional = true } }) }, scope);
        _collection = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, model with { Properties = model.Properties.SetItem(0, property with { Type = property.Type with { IsCollection = true } }) }, scope);
        _seed = SemanticRunAdmission.Check(plan, spec with { GivenReadModels = [new(model.Id, SemanticValue.Null, [])] })?.Details;
        _typedComposite = Reject(new SemanticProjectionCompositeKey(model.Id, parts.Parts));
        var invoice = model.Properties.Single(candidate => candidate.Name == "invoice");
        var uuidModel = model with { Properties = model.Properties.Replace(invoice, invoice with { Type = invoice.Type with { Primitive = SemanticPrimitiveType.Uuid } }) };
        _mixedFrom = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, uuidModel, scope with { Removals = [] });
        _mixedRemoval = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, uuidModel, scope with { From = [] });

        // Chronicle keys composite parts by their text, so 1.0 and 1.00 would become different rows.
        var decimalModel = model with { Properties = model.Properties.Replace(invoice, invoice with { Type = invoice.Type with { Primitive = SemanticPrimitiveType.DecimalNumber } }) };
        _decimal = SemanticRunProjectionAdmission.CheckCompositeKey(plan, spec, decimalModel, scope);
    }

    [Fact] void should_admit_the_bounded_root_projection() => _supported.ShouldBeNull();
    [Fact] void should_refuse_missing_parts() => _missing.ShouldNotBeNull();
    [Fact] void should_refuse_repeated_parts() => _duplicate.ShouldNotBeNull();
    [Fact] void should_refuse_literal_parts() => _literal.ShouldNotBeNull();
    [Fact] void should_refuse_parent_keys() => _parent.ShouldNotBeNull();
    [Fact] void should_refuse_optional_parts() => _optional.ShouldNotBeNull();
    [Fact] void should_refuse_collection_parts() => _collection.ShouldNotBeNull();
    [Fact] void should_explain_seed_refusal_before_scalar_seed_lookup() => _seed!.ShouldContain("Given readmodel seeds with composite keys");
    [Fact] void should_keep_typed_composite_keys_refused() => _typedComposite.ShouldNotBeNull();
    [Fact] void should_refuse_text_to_uuid_from_parts() => _mixedFrom!.ShouldContain("compatible required event properties");
    [Fact] void should_refuse_decimal_parts_whose_scale_would_split_rows() => _decimal!.ShouldContain("non-decimal");
    [Fact] void should_refuse_text_to_uuid_removal_parts() => _mixedRemoval!.ShouldContain("compatible required event properties");

    const string Source = """
        module Billing
          feature Payments
            slice StateView Status
              event Paid
                tenant String
                invoice String
                amount Int
              event Cancelled
                tenant String
                invoice String
              readmodel Payment
                tenant String key
                invoice String key
                amount Int
              projection Payments => Payment
                from Paid
                  key
                    tenant = tenant
                    invoice = invoice
                  amount = amount
                remove with Cancelled
                  key
                    tenant = tenant
                    invoice = invoice
              specification Existing
                given Paid
                  tenant = "north"
                  invoice = "first"
                  amount = 1
                then readmodel Payment
                  tenant = "north"
                  invoice = "first"
                  amount = 1
        """;
}
