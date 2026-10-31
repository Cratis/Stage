// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class an_observable_lookup_probe
{
    public const string Source = """
        using System.Linq.Expressions;
        using System.Reactive;
        using System.Reactive.Disposables;
        using System.Reactive.Linq;
        using System.Reactive.Subjects;
        using System.Reactive.Threading.Tasks;
        using Cratis.Arc.Queries;
        using Cratis.Arc.Testing.Queries;
        using Microsoft.Extensions.DependencyInjection;
        using Cratis.Chronicle.ReadModels;
        using Cratis.Chronicle.Testing.Events;
        using Invoices.Billing.Invoicing.Issue;
        using Invoices.Billing.Invoicing.Lookup;
        using Invoices.Common;
        using Xunit;

        namespace Invoices.Probes;

        public sealed class TrackingReadModels(IReadModels inner, ISubject<IEnumerable<InvoiceSummary>> input)
            : Cratis.Chronicle.Testing.ReadModels.ReadModelsForTesting(inner), IReadModels
        {
            public int ActiveSubscriptions { get; private set; }

            public new ISubject<IEnumerable<T>> Observe<T>(Expression<Func<T, bool>>? filter = null)
            {
                var predicate = filter?.Compile();
                return System.Reactive.Subjects.Subject.Create<IEnumerable<T>>(
                    Observer.Create<IEnumerable<T>>(rows => input.OnNext(rows.Cast<InvoiceSummary>()), input.OnError, input.OnCompleted),
                    Observable.Create<IEnumerable<T>>(observer =>
                    {
                        ActiveSubscriptions++;
                        var subscription = input.Select(rows => predicate is null ? rows.Cast<T>() : rows.Cast<T>().Where(predicate)).Subscribe(observer);
                        return Disposable.Create(() => { subscription.Dispose(); ActiveSubscriptions--; });
                    }));
            }
        }

        public class observable_lookups
        {
            [Fact]
            public async Task should_replay_the_matching_instance_and_observe_later_updates()
            {
                using var store = new EventStoreForTesting();
                Assert.True((await store.EventLog.Append("invoice-two", new InvoiceIssued(new InvoiceId("invoice-two"), "Other"))).IsSuccess);
                var query = InvoiceSummary.InvoiceById(store.ReadModels, new InvoiceId("invoice-one"));
                var values = new List<InvoiceSummary?>();
                using var subscription = query.Subscribe(values.Add);
                Assert.Single(values);
                Assert.Null(values[0]);
                var inserted = query.FirstAsync(value => value?.Description == "North").ToTask();
                Assert.True((await store.EventLog.Append("invoice-one", new InvoiceIssued(new InvoiceId("invoice-one"), "North"))).IsSuccess);
                Assert.Equal("North", (await inserted.WaitAsync(TimeSpan.FromSeconds(10)))!.Description);
                var updated = query.FirstAsync(value => value?.Description == "Updated").ToTask();
                Assert.True((await store.EventLog.Append("invoice-one", new InvoiceIssued(new InvoiceId("invoice-one"), "Updated"))).IsSuccess);
                Assert.Equal("Updated", (await updated.WaitAsync(TimeSpan.FromSeconds(10)))!.Description);
                Assert.Contains(values, value => value?.Description == "North");
                Assert.Contains(values, value => value?.Description == "Updated");
                Assert.DoesNotContain(values, value => value?.InvoiceId == new InvoiceId("invoice-two"));
            }

            [Fact]
            public void should_preserve_null_transitions_and_dispose_the_underlying_subscription()
            {
                using var input = new BehaviorSubject<IEnumerable<InvoiceSummary>>([]);
                using var store = new EventStoreForTesting();
                var readModels = new TrackingReadModels(store.ReadModels, input);
                var query = InvoiceSummary.InvoiceById(readModels, new InvoiceId("invoice-one"));
                Assert.Equal(0, readModels.ActiveSubscriptions);
                var values = new List<InvoiceSummary?>();
                var subscription = query.Subscribe(values.Add);
                Assert.Equal(1, readModels.ActiveSubscriptions);
                var row = new InvoiceSummary(new InvoiceId("invoice-one"), "North");
                input.OnNext([new InvoiceSummary(new InvoiceId("invoice-two"), "Other"), row]);
                input.OnNext([row with { Description = "Updated" }]);
                input.OnNext([]);
                Assert.Equal(new string?[] { null, "North", "Updated", null }, values.Select(value => value?.Description));
                subscription.Dispose();
                Assert.Equal(0, readModels.ActiveSubscriptions);
                input.OnNext([row]);
                Assert.Equal(4, values.Count);
            }

            [Fact]
            public async Task should_return_a_single_nullable_value_through_arcs_query_pipeline()
            {
                using var store = new EventStoreForTesting();
                using var scenario = new QueryScenario<InvoiceSummary>();
                scenario.Services.AddSingleton<IReadModels>(store.ReadModels);
                var missing = await scenario.Perform(nameof(InvoiceSummary.InvoiceById), new QueryArguments { ["invoiceId"] = "invoice-one" });
                Assert.False(missing.HasExceptions, string.Join("; ", missing.ExceptionMessages));
                Assert.True(missing.IsValid);
                Assert.True(missing.IsAuthorized);
                Assert.Null(missing.Data);
                var row = new InvoiceSummary(new InvoiceId("invoice-one"), "North");
                Assert.True((await store.EventLog.Append("invoice-one", new InvoiceIssued(new InvoiceId("invoice-one"), "North"))).IsSuccess);
                Assert.True((await store.EventLog.Append("invoice-two", new InvoiceIssued(new InvoiceId("invoice-two"), "Other"))).IsSuccess);
                var found = await scenario.Perform(nameof(InvoiceSummary.InvoiceById), new QueryArguments { ["invoiceId"] = "invoice-one" });
                Assert.False(found.HasExceptions, string.Join("; ", found.ExceptionMessages));
                Assert.True(found.IsValid);
                Assert.True(found.IsAuthorized);
                Assert.Equal(row, Assert.IsType<InvoiceSummary>(found.Data));
            }
        }
        """;
}
