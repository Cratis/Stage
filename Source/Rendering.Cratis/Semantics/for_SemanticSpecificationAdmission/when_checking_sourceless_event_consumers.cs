// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_checking_sourceless_event_consumers
{
    internal const string Source = """
        module Billing
          feature Payments
            slice StateView Totals
              event PaymentReceived
                invoiceId String
                amount Int
              readmodel Totals
                invoiceId String
                total Int
              projection TotalsProjection => Totals
                from PaymentReceived key invoiceId
                  invoiceId = invoiceId
                  total = amount
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
    [InlineData("parent")]
    [InlineData("mapping")]
    [InlineData("every")]
    [InlineData("all")]
    [InlineData("child")]
    [InlineData("reducer")]
    [InlineData("constraint")]
    [InlineData("release")]
    [InlineData("route")]
    void should_refuse_each_source_observer(string observer)
    {
        var diagnostics = Validate(slice => Observe(slice, observer));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051");
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
            "key" => Projection(scope with { From = [fromEvent with { Key = SemanticProjectionKey.EventSourceIdentity }] }),
            "parent" => Projection(scope with { From = [fromEvent with { ParentKey = SemanticProjectionKey.EventSourceIdentity }] }),
            "mapping" => Projection(scope with { From = [fromEvent with { Mappings = [fromEvent.Mappings[0] with { Source = new SemanticProjectionEventSourceIdentity() }] }] }),
            "every" => Projection(scope with { Every = new(true, false, [fromEvent.Mappings[0] with { Source = new SemanticProjectionEventSourceIdentity() }]) }),
            "all" => Projection(scope with { Every = new(true, true, []) }),
            "child" => Projection(scope with { From = [], Children = [new(fromEvent.Mappings[0].Target[0], fromEvent.Mappings[0].Target[0], scope)] }),
            "reducer" => slice with { Reducers = [new("TotalsReducer", slice.ReadModels.Single().Id, [new(contract, "Reduce")])] },
            "constraint" => slice with { Constraints = [new("UniquePayment", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(contract, [])], [], false, null)] },
            "release" => slice with { Constraints = [new("UniquePayment", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [], [contract], false, null)] },
            "route" => slice with { Specifications = [slice.Specifications[0] with { GivenEvents = [slice.Specifications[0].GivenEvents[0] with { Route = new(contract, contract) }] }] },
            _ => throw new ArgumentOutOfRangeException(nameof(observer))
        };

        SemanticSlice Projection(SemanticProjectionScope changed) => slice with { Projections = [projection with { Scope = changed }] };
    }

    static List<ArtifactRenderDiagnostic> Validate(Func<SemanticSlice, SemanticSlice> change)
    {
        var model = invoice_model.Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = change(feature.Slices.Single());
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        var changedModel = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        var options = new CratisRenderingOptions("Payments", "Payments");
        var context = new SemanticApplicationContext(
            new(changedModel, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(application.Name, options), new(ArtifactRenderScopeKind.Application, application.Id)),
            options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, slice, diagnostics);
        return diagnostics;
    }
}
