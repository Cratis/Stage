// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_validated_query_key_model
{
    public const string Source = """
        concept InvoiceId : String
          validate
            min 2
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              event InvoiceIssued
                invoiceId InvoiceId
                description String
            slice StateView Lookup
              readmodel InvoiceSummary
                invoiceId InvoiceId
                description String
              query InvoiceById => InvoiceSummary?
                by invoiceId InvoiceId
              query InvoicesById => observable InvoiceSummary[]
                by invoiceId InvoiceId
              projection InvoiceSummaryProjection => InvoiceSummary
                from InvoiceIssued key invoiceId
                  invoiceId = invoiceId
                  description = description
        """;

    public const string Probe = """
        using Cratis.Arc.Queries;
        using Cratis.Arc.Testing.Queries;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.ReadModels;
        using Invoices.Billing.Invoicing.Lookup;
        using Invoices.Common;
        using Microsoft.Extensions.DependencyInjection;
        using NSubstitute;
        using Xunit;

        namespace Invoices.Probes;

        public sealed class QueryReadModels(IReadModels inner, IEnumerable<InvoiceSummary> rows) : Cratis.Chronicle.Testing.ReadModels.ReadModelsForTesting(inner), IReadModels
        {
            readonly IReadModels _inner = inner;

            public new Task<T> GetInstanceById<T>(ReadModelKey key, ReadModelSessionId? sessionId = null) => _inner.GetInstanceById<T>(key, sessionId);

            public new System.Reactive.Subjects.ISubject<IEnumerable<T>> Observe<T>(System.Linq.Expressions.Expression<Func<T, bool>>? filter = null) =>
                new System.Reactive.Subjects.BehaviorSubject<IEnumerable<T>>(filter is null ? rows.Cast<T>().ToArray() : rows.Cast<T>().Where(filter.Compile()).ToArray());
        }

        public static class QueryKeys
        {
            public static async Task<QueryResult[]> Execute()
            {
                var id = new InvoiceId("invoice-one");
                var invalidId = new InvoiceId("x");
                var instance = new InvoiceSummary(InvoiceId: id, Description: "North");
                var readModels = Substitute.For<IReadModels>();
                readModels.GetInstanceById<InvoiceSummary>((EventSourceId)id).Returns(instance);
                var unmatchedId = new InvoiceId("invoice-missing");
                readModels.GetInstanceById<InvoiceSummary>((EventSourceId)unmatchedId).Returns(Task.FromResult<InvoiceSummary>(null!));
                InvoiceSummary[] rows = [instance];

                // Arc's Chronicle interceptor releases each returned instance before assigning Data.
                // This model has no protected properties, so release must preserve the instance.
                readModels.Release<InvoiceSummary>(Arg.Any<InvoiceSummary>()).Returns(call => Task.FromResult(call.Arg<InvoiceSummary>()));
                using var lookupScenario = new QueryScenario<InvoiceSummary>();
                using var principalScope1 = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                // Snapshot reads normally require a silo. Keep the real Arc argument conversion
                // and validation pipeline, but substitute the store as companion specs do.
                lookupScenario.Services.AddSingleton<IReadModels>(new QueryReadModels(readModels, rows));
                var valid = new QueryArguments { ["invoiceId"] = "invoice-one" };
                var invalid = new QueryArguments { ["invoiceId"] = "x" };
                var lookup = await lookupScenario.Perform(nameof(InvoiceSummary.InvoiceById), valid);
                var rejected = await lookupScenario.Perform(nameof(InvoiceSummary.InvoiceById), invalid);
                var missing = await lookupScenario.Perform(nameof(InvoiceSummary.InvoiceById), new QueryArguments { ["invoiceId"] = "invoice-missing" });
                await readModels.Received(1).GetInstanceById<InvoiceSummary>((EventSourceId)id);
                await readModels.DidNotReceive().GetInstanceById<InvoiceSummary>((EventSourceId)invalidId);
                await readModels.Received(1).GetInstanceById<InvoiceSummary>((EventSourceId)unmatchedId);
                var filtered = await lookupScenario.Perform(nameof(InvoiceSummary.InvoicesById), valid);
                var rejectedFilter = await lookupScenario.Perform(nameof(InvoiceSummary.InvoicesById), invalid);
                var empty = await lookupScenario.Perform(nameof(InvoiceSummary.InvoicesById), new QueryArguments { ["invoiceId"] = "invoice-missing" });
                Assert.All(new[] { rejected, rejectedFilter }, result =>
                {
                    Assert.False(result.IsValid);
                    Assert.NotEmpty(result.ValidationResults);
                    Assert.False(result.HasExceptions);
                    Assert.Null(result.Data);
                });
                Assert.All(new[] { lookup, missing, filtered, empty }, result =>
                {
                    Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                    Assert.Empty(result.ValidationResults);
                    Assert.True(result.IsValid);
                    Assert.True(result.IsAuthorized);
                });
                Assert.Same(instance, Assert.IsType<InvoiceSummary>(lookup.Data));
                Assert.Null(missing.Data);
                // The query pipeline's interception stage materializes collections as object[].
                Assert.Same(instance, Assert.IsType<InvoiceSummary>(Assert.Single(Assert.IsType<IEnumerable<object>>(filtered.Data, exactMatch: false))));
                Assert.Empty(Assert.IsType<IEnumerable<object>>(empty.Data, exactMatch: false));
                return [lookup, rejected, filtered, rejectedFilter];
            }
        }
        """;
}
