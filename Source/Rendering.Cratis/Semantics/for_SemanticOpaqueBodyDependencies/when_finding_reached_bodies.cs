// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOpaqueBodyDependencies;

public class when_finding_reached_bodies
{
    [Fact]
    public void should_find_the_named_rule()
    {
        var (context, specification) = Context("return context.Value > 0;", false, false);
        Assert.Equal(SemanticOpaqueBodyKind.RulePredicate, Assert.Single(SemanticOpaqueBodyDependencies.Reached(context, specification)).Kind);
        Assert.Null(SemanticOpaqueBodyDependencies.Rejection(context, specification));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_require_a_clock_only_for_receipt_time(bool clock)
    {
        var (context, specification) = Context("return (DateTimeOffset)context.Value > context.Occurred;", true, clock);
        var rejection = SemanticOpaqueBodyDependencies.Rejection(context, specification);
        if (clock) Assert.Null(rejection);
        else Assert.Contains("rule 'Positive' reads context.Occurred; add `given clock`", rejection, StringComparison.Ordinal);
    }

    [Fact]
    public void should_find_no_bodies_on_a_plain_command()
    {
        var loaded = validation_body_model.Load("""
            concept Id : String
            module Test
              feature Test
                slice StateChange Test
                  command Do
                    id Id identifier
                    produces Done
                      for id
                  event Done
                  specification Done
                    when Do
                      id = "one"
                    then Done
                      for "one"
            """);
        var context = CreateContext(loaded);
        var specification = context.SelectedSlices().Single().Slice.Specifications.Single();
        Assert.Empty(SemanticOpaqueBodyDependencies.Reached(context, specification));
    }

    static (SemanticApplicationContext Context, global::Cratis.Screenplay.Semantics.SemanticSpecification Specification) Context(string body, bool date, bool clock)
    {
        var source = validation_body_model.Command(body);
        if (date) source = source.Replace("amount Decimal", "amount DateTime", StringComparison.Ordinal);
        source += "\n      specification Decide\n" + (clock ? "        given clock \"2026-06-01T09:00:00Z\"\n" : string.Empty) + """
                    when PlaceOrder
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            """ + "\n          amount = " + (date ? "\"2026-06-02T09:00:00Z\"" : "0") + "\n        then error \"Positive amount required\"";
        var context = CreateContext(validation_body_model.Load(source));
        return (context, context.SelectedSlices().Single().Slice.Specifications.Single());
    }

    static SemanticApplicationContext CreateContext(LoadedSemanticModel loaded) => new(
        new(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile(loaded.Model.Application.Name, new("InvoiceApp", "Invoices")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        },
        new("InvoiceApp", "Invoices"));
}
#endif
