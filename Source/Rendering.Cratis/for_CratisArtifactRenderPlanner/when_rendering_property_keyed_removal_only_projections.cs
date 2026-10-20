// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_property_keyed_removal_only_projections : Specification
{
    internal const string Source = """
        module Billing
          feature Invoices
            slice StateChange RemoveInvoice
              command RemoveInvoice
                sourceId String identifier
                invoiceId String
                produces InvoiceRemoved
                  for sourceId
                  invoiceId = invoiceId
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String key
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
              specification RemovingOneOfTwoInvoices
                given InvoiceRemoved
                  for "unrelated"
                  invoiceId = "second"
                given readmodel InvoiceView
                  invoiceId = "first"
                given readmodel InvoiceView
                  invoiceId = "second"
                when append InvoiceRemoved
                  for "unrelated"
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
                then readmodel InvoiceView
                  invoiceId = "second"
        """;

    const string ExplicitArtifacts = """
         _scenario = new(default, new global::Cratis.Chronicle.Testing.Defaults(new global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(global::Invoices.Billing.Invoices.InvoiceLookup.InvoiceRemoved)],
            Projections = [typeof(global::Invoices.Billing.Invoices.InvoiceLookup.Invoices)]
        }));
        """;

    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(Source));
        _files = when_rendering_read_model_absence.Files(_plan);
    }

    [Fact] void should_admit_property_keyed_removal() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_render_the_authored_property_key() => _files.Any(file => file.Content.Contains(".UsingKey(evt => evt.InvoiceId)", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_without_errors() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_files)).ShouldEqual(string.Empty);
    [Fact] async Task should_replace_history_with_seeds_then_remove_only_the_action_key() => await Run(_files);

    [Fact] async Task should_also_allow_an_unsourced_property_keyed_append()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Source.Replace("        when append InvoiceRemoved\n          for \"unrelated\"", "        when append InvoiceRemoved", StringComparison.Ordinal)));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        await Run(when_rendering_read_model_absence.Files(plan));
    }

    [Fact] async Task should_fail_when_the_action_removes_the_wrong_key()
    {
        var source = Source.Replace("        when append InvoiceRemoved\n          for \"unrelated\"\n          invoiceId = \"first\"", "        when append InvoiceRemoved\n          for \"unrelated\"\n          invoiceId = \"second\"", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = when_rendering_read_model_absence.Files(plan);
        string.Join(Environment.NewLine, RenderedOutput.Errors(files)).ShouldEqual(string.Empty);
        var assembly = when_rendering_read_model_absence.LoadWithArtifacts(files, ExplicitArtifacts);
        var failure = await Catch.Exception(() => when_rendering_read_model_absence.RunProjected(assembly));
        Assert.IsType<global::Xunit.Sdk.XunitException>(Assert.IsType<TargetInvocationException>(failure).InnerException, exactMatch: false);
    }

    static Task Run(RenderedFile[] files) => when_rendering_read_model_absence.RunProjected(when_rendering_read_model_absence.LoadWithArtifacts(files, ExplicitArtifacts));
}
#endif
