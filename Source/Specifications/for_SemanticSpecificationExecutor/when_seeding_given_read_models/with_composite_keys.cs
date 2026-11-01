// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_composite_keys : Specification
{
    internal const string Source = """
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
              event Cancelled
                tenant TenantId
                method PaymentMethod
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
                remove with Cancelled
                  key
                    tenant = tenant
                    method = method
              specification SeedOnly
                given readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                then readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
              specification ReplacesOnlyMatchingHistory
                given Paid
                  for "unrelated-stream"
                  tenant = "north"
                  method = "card"
                  amount = 1
                given Paid
                  for "same-stream"
                  tenant = "north"
                  method = "cash"
                  amount = 2
                given readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                then readmodel Total
                  tenant = "north"
                  method = "card"
                  amount = 42
                then readmodel Total
                  tenant = "north"
                  method = "cash"
                  amount = 2
              specification UpdatesOnlyMatchingSeed
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

    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunReport _repeated = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeSeeds"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("CompositeSeeds", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        var executor = new SemanticSpecificationExecutor();
        _report = await executor.Run(_plan, new([]), new());
        _repeated = await executor.Run(_plan, new([]), new());
    }

    [Fact] void should_run_each_composite_seed_world() => Assert.All(_report.Results, result => Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Name}: {result.Outcome}; {result.Unsupported?.Details}; {string.Join(';', result.Failures)}"));
    [Fact] void should_cover_three_distinct_worlds() => _report.Results.Count.ShouldEqual(3);
    [Fact] void should_match_the_reference() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
    [Fact] void should_repeat_without_leaking_seeds() => _repeated.Results.Select(result => result.Outcome).ShouldEqual(_report.Results.Select(result => result.Outcome));
}
