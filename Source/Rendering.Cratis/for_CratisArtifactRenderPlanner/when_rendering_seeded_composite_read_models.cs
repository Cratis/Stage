// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_seeded_composite_read_models : Specification
{
    const string Source = """
        concept TenantId : String
        concept PaymentMethod : Enum
          card
          cash
        module Billing
          feature Payments
            slice StateView Totals
              command RecordPayment
                stream String identifier
                tenant TenantId
                method PaymentMethod
                amount Int
                produces Paid
                  for stream
                  tenant = tenant
                  method = method
                  amount = amount
              event Paid
                tenant TenantId
                method PaymentMethod
                amount Int
              readmodel Total
                tenant TenantId key
                method PaymentMethod key
                amount Int
              projection Totals => Total
                no automap
                from Paid
                  key
                    tenant = tenant
                    method = method
                  amount = amount
              specification SeedOnly
                given readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                then readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
              specification ReplacesHistory
                given Paid
                  for "unrelated-stream"
                  tenant = "north"
                  method = "card"
                  amount = 1
                given readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                then readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
              specification UpdatesSeed
                given readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                given readmodel Total
                  tenant = "north"
                  method = "cash"
                  amount = 2
                when append Paid
                  for "unrelated-stream"
                  tenant = "north"
                  method = "card"
                  amount = 99
                then readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 99
                then readmodel Total
                  tenant = "north"
                  method = "cash"
                  amount = 2
        """;

    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];
    RenderedFile[] _repeated = [];

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(Source));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _files = when_rendering_read_model_absence.Files(_plan);
        _repeated = when_rendering_read_model_absence.Files(invoice_model.Plan(invoice_model.Compile(Source)));
    }

    [Fact] void should_admit_composite_seeds() => _plan.Success.ShouldBeTrue();
    [Fact] void should_use_named_key_parts() => _files.Any(file => file.Content.Contains(".Given.ForKey(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_treat_parts_as_event_sources() => _files.Any(file => file.Content.Split('\n').Any(line => line.Contains(".ForEventSource(", StringComparison.Ordinal) && line.Contains(".ReadModel(", StringComparison.Ordinal))).ShouldBeFalse();
    [Fact] void should_construct_the_synthetic_id() => _files.Any(file => file.Content.Contains("(Id: new ", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_use_the_schema_naming_policy() => _files.Any(file => file.Content.Contains("CamelCaseNamingPolicy().GetPropertyName(nameof(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_regenerate_identical_bytes() => _repeated.ShouldEqual(_files);
}
#endif
