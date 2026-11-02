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

public class when_rendering_projected_read_models_with_given_read_models : Specification
{
    internal const string Source = """
        concept InvoiceId : String
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
              specification CommandUpdatesSeed
                given readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
                when IssueInvoice
                  invoiceId = "first"
                  description = "Updated"
                then InvoiceIssued
                  invoiceId = "first"
                  description = "Updated"
                then readmodel InvoiceView
                  invoiceId = "first"
                  description = "Updated"
                  visits = 43
            slice StateView Browse
              readmodel InvoiceView
                invoiceId InvoiceId
                description String
                visits Int
              query ById => InvoiceView optional
                by invoiceId InvoiceId
              projection Invoices => InvoiceView
                from InvoiceIssued key invoiceId
                  description = description
                  increment visits
              specification SeedOnly
                given readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
                then readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
              specification ReplacesHistory
                given InvoiceIssued
                  for "unrelated-stream"
                  invoiceId = "first"
                  description = "History"
                given InvoiceIssued
                  for "another-stream"
                  invoiceId = "second"
                  description = "Other"
                given readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
                then readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
                then readmodel InvoiceView
                  invoiceId = "second"
                  description = "Other"
                  visits = 1
              specification AppendUpdatesSeed
                given InvoiceIssued
                  for "history-stream"
                  invoiceId = "first"
                  description = "History"
                given readmodel InvoiceView
                  invoiceId = "first"
                  description = "Fixture"
                  visits = 42
                when append InvoiceIssued
                  for "unrelated-stream"
                  invoiceId = "first"
                  description = "Updated"
                then readmodel InvoiceView
                  invoiceId = "first"
                  description = "Updated"
                  visits = 43
        """;

    const string ExplicitArtifacts = """
         _scenario = new(default, new global::Cratis.Chronicle.Testing.Defaults(new global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(global::Invoices.Billing.Invoicing.Issue.InvoiceIssued)],
            Projections = [typeof(global::Invoices.Billing.Invoicing.Browse.Invoices)]
        }));
        """;

    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    void Establish() => _plan = invoice_model.Plan(invoice_model.Compile(Source));

    void Because() => _files = when_rendering_read_model_absence.Files(_plan);

    [Fact] void should_admit_positive_scalar_seed_expectations() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_not_refuse_any_specification() => _plan.Refusals.ShouldBeEmpty();
    [Fact] void should_emit_the_keyed_fixture() => _files.Any(file => file.Content.Contains(".ReadModel(new ", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_the_positive_assertion() => _files.Any(file => file.Content.Contains("should_project_visits", StringComparison.Ordinal)).ShouldBeTrue();

    [Theory]
    [InlineData("SeedOnly")]
    [InlineData("ReplacesHistory")]
    [InlineData("AppendUpdatesSeed")]
    [InlineData("CommandUpdatesSeed")]
    public void should_emit_every_specification(string name) =>
        _files.Any(file => file.Content.Contains(Naming.Identifiers.ToSnakeCase(name), StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] void should_compile_without_errors() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_files)).ShouldEqual(string.Empty);
    [Fact] async Task should_replace_history_and_fold_command_and_append_events_over_the_seed() => await when_rendering_read_model_absence.RunProjected(Load(_files));

    [Fact]
    async Task should_fail_an_incorrect_positive_expectation()
    {
        var files = _files.Select(file => file with { Content = file.Content.Replace(".ShouldEqual(43)", ".ShouldEqual(44)", StringComparison.Ordinal) }).ToArray();
        var failure = await Catch.Exception(() => when_rendering_read_model_absence.RunProjected(Load(files)));
        Assert.IsType<global::Xunit.Sdk.XunitException>(Assert.IsType<TargetInvocationException>(failure).InnerException, exactMatch: false);
    }

    static Assembly Load(IEnumerable<RenderedFile> files) => when_rendering_read_model_absence.LoadWithArtifacts(files, ExplicitArtifacts);
}
#endif
