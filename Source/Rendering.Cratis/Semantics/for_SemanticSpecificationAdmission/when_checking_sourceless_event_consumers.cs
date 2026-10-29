// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_checking_sourceless_event_consumers
{
    internal const string Source = """
        concept InvoiceId : String
        eventsource PaymentsSource
          identifier InvoiceId
          stream Entries
        module Billing
          feature Payments
            slice StateView Totals
              event PaymentReceived
                invoiceId String
                amount Int
              readmodel Totals
                invoiceId String
                total Int
              query TotalsByInvoice => Totals?
                by invoiceId String
              projection TotalsProjection => Totals
                from PaymentReceived key invoiceId
                  invoiceId = invoiceId
                  total = amount
                every
                  total = 20
              specification Recreate
                given PaymentReceived
                  invoiceId = "INV-1"
                  amount = 12
                when append PaymentReceived
                  invoiceId = "INV-1"
                  amount = 20
                then readmodel Totals
                  invoiceId = "INV-1"
                  total = 20
        """;

    [Fact]
    void should_admit_an_explicit_property_key() => Assert.Empty(Validate(slice => slice));

    [Theory]
    [InlineData("key")]
    [InlineData("mapping")]
    [InlineData("every")]
    [InlineData("all")]
    [InlineData("reducer")]
    [InlineData("constraint")]
    [InlineData("release")]
    void should_refuse_each_source_observer(string observer)
    {
        var diagnostics = Validate(slice => Observe(slice, observer));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051");
    }

    [Fact]
    void should_refuse_a_child_parent_source()
    {
        var diagnostics = Validate(composite_source_models.Child(true));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051");
    }

    [Fact]
    void should_refuse_a_sourceless_route_at_the_semantic_boundary()
    {
        var exception = Assert.Throws<InvalidSemanticContract>(ValidateRoutedGiven);
        Assert.Contains("requires an event source identity", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    void should_refuse_a_reaction_trigger()
    {
        var model = invoice_model.Compile(Source + "\n" + """
            slice Automation Notify
              event Notified
                invoiceId String
              reaction Notify
                when PaymentReceived
                  produces Notified
                    for invoiceId
                    invoiceId = invoiceId
        """);
        Assert.Contains(invoice_model.Plan(model).Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051");
    }

    [Fact]
    void should_refuse_an_observable_sourceless_append()
    {
        var diagnostics = Validate(slice => Observe(slice, "key") with
        {
            Specifications = [slice.Specifications[0] with { GivenEvents = [] }]
        });
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051" && diagnostic.Message.Contains("appended event", StringComparison.Ordinal));
    }

    static SemanticSlice Observe(SemanticSlice slice, string observer)
    {
        var projection = slice.Projections.Single();
        var scope = projection.Scope!;
        var fromEvent = scope.From.Single();
        var contract = slice.Events.Single().Id;
        return observer switch
        {
            "reducer" => slice with { Reducers = [new("TotalsReducer", slice.ReadModels.Single().Id, [new(contract, "Reduce")])] },
            "constraint" => slice with { Constraints = [new("UniquePayment", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(contract, [])], [], false, null)] },
            "release" => WithRelease(slice, contract),
            _ => Projection(ObserveProjection(observer, scope, fromEvent))
        };

        SemanticSlice Projection(SemanticProjectionScope changed) => slice with { Projections = [projection with { Scope = changed }] };
    }

    static SemanticProjectionScope ObserveProjection(
        string observer,
        SemanticProjectionScope scope,
        SemanticProjectionFrom fromEvent)
    {
        return observer switch
        {
            "key" => scope with { From = [fromEvent with { Key = SemanticProjectionKey.EventSourceIdentity }] },
            "mapping" => scope with { From = [fromEvent with { Mappings = [fromEvent.Mappings[0] with { Source = new SemanticProjectionEventSourceIdentity() }] }] },
            "every" => scope with { Every = new(true, false, [fromEvent.Mappings[0] with { Source = new SemanticProjectionEventSourceIdentity() }]) },
            "all" => scope with { Every = new(true, true, []) },
            _ => throw new ArgumentOutOfRangeException(nameof(observer))
        };
    }

    static List<ArtifactRenderDiagnostic> Validate(Func<SemanticSlice, SemanticSlice> change)
    {
        var model = invoice_model.Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = change(feature.Slices.Single());
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        return Validate(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application));
    }

    static List<ArtifactRenderDiagnostic> Validate(ExecutableSemanticModel model)
    {
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var options = new CratisRenderingOptions("Payments", "Payments");
        var context = new SemanticApplicationContext(
            new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)),
            options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, slice, diagnostics);
        return diagnostics;
    }

    static SemanticSlice WithRelease(SemanticSlice slice, SemanticId releasedBy)
    {
        var reserved = invoice_model.Compile(Source.Replace("PaymentReceived", "PaymentReserved", StringComparison.Ordinal));
        var constrained = reserved.Application.Modules.Single().Features.Single().Slices.Single().Events.Single();
        return slice with
        {
            Events = slice.Events.Add(constrained),
            Constraints = [new("UniquePayment", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(constrained.Id, [])], [releasedBy], false, null)]
        };
    }

    static List<ArtifactRenderDiagnostic> ValidateRoutedGiven()
    {
        var model = invoice_model.Compile(Source);
        var source = model.Application.EventSources.Single();
        var route = new SemanticFixtureRoute(source.Id, source.Streams.Single().Id);
        return Validate(slice => slice with
        {
            Specifications = [slice.Specifications[0] with { GivenEvents = [slice.Specifications[0].GivenEvents[0] with { Route = route }] }]
        });
    }
}
