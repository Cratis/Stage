// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_read_model_absence : Specification
{
    internal const string Source = """
        concept InvoiceId : String
        module Billing
          feature Invoicing
            slice StateChange Remove
              command IssueInvoice
                invoiceId InvoiceId identifier
                description String
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
                  description = description
              command RemoveInvoice
                invoiceId InvoiceId identifier
                produces InvoiceRemoved
                  for invoiceId
              event InvoiceIssued
                invoiceId InvoiceId
                description String
              event InvoiceRemoved
              specification RemovingAnInvoice
                given InvoiceIssued
                  for "first"
                  invoiceId = "first"
                  description = "First"
                given InvoiceIssued
                  for "second"
                  invoiceId = "second"
                  description = "Second"
                when RemoveInvoice
                  invoiceId = "first"
                then InvoiceRemoved
                then readmodel InvoiceView
                  invoiceId = "second"
                  description = "Second"
                then no readmodel InvoiceView for "first"
                then no readmodel InvoiceView for "third"
            slice StateView Browse
              readmodel InvoiceView
                invoiceId InvoiceId
                description String
              query ById => InvoiceView?
                by invoiceId InvoiceId
              projection Invoices => InvoiceView
                from InvoiceIssued key invoiceId
                  description = description
                remove with InvoiceRemoved
              specification RemovingAFact
                given InvoiceIssued
                  for "first"
                  invoiceId = "first"
                  description = "First"
                when append InvoiceRemoved
                  for "first"
                then no readmodel InvoiceView for "first"
              specification NoInvoiceYet
                then no readmodel InvoiceView for "first"
        """;

    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    void Because()
    {
        _plan = invoice_model.Plan(invoice_model.Compile(Source));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _files = Files(_plan);
    }

    [Fact] void should_admit_keyed_absence() => _plan.Success.ShouldBeTrue();
    [Fact] void should_emit_each_absence_assertion() => _files.Count(file => file.Content.Contains("should_not_project_the_absent_instance", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_keep_the_presence_assertions() => _files.Any(file => file.Content.Contains("should_project_description", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_observe_the_keyed_collection() => _files.Any(file => file.Content.Contains("_scenario.Instances.ContainsKey(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_against_the_pinned_scenario_api() => string.Join(Environment.NewLine, RenderedOutput.Errors(_files)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_files)).ShouldEqual(string.Empty);
    [Fact] async Task should_pass_absence_and_presence_after_replay() => await RunProjected(LoadWithArtifacts(_files));
    [Fact] async Task should_fail_absence_at_an_existing_key()
    {
        var source = Source.Replace("then no readmodel InvoiceView for \"third\"", "then no readmodel InvoiceView for \"second\"", StringComparison.Ordinal)
            .Replace("        then readmodel InvoiceView\n          invoiceId = \"second\"\n          description = \"Second\"\n", string.Empty, StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var failure = await Catch.Exception(() => RunProjected(LoadWithArtifacts(Files(plan))));
        Assert.IsType<global::Xunit.Sdk.XunitException>(Assert.IsType<TargetInvocationException>(failure).InnerException, exactMatch: false);
    }

    internal static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];

    const string ExplicitArtifacts = """
         _scenario = new(default, new global::Cratis.Chronicle.Testing.Defaults(new global::Cratis.Chronicle.Testing.InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(global::Invoices.Billing.Invoicing.Remove.InvoiceIssued), typeof(global::Invoices.Billing.Invoicing.Remove.InvoiceRemoved)],
            Projections = [typeof(global::Invoices.Billing.Invoicing.Browse.Invoices)]
        }));
        """;

    static Assembly LoadWithArtifacts(IEnumerable<RenderedFile> files) => RenderedOutput.Load(files.Select(file => file with
    {
        // The dynamically loaded assembly is outside the process-wide discovery snapshot.
        Content = file.RelativePath.Contains("_is_projected", StringComparison.Ordinal)
            ? file.Content.Replace(" _scenario = new();", ExplicitArtifacts, StringComparison.Ordinal)
            : file.Content
    }));

    static async Task RunProjected(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(type => type.Name.Contains("_is_projected", StringComparison.Ordinal)).ToArray();
        types.ShouldNotBeEmpty();
        foreach (var type in types)
        {
            using var instance = (IDisposable)Activator.CreateInstance(type)!;
            var establishment = type.GetMethod("Establish", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, null);
            if (establishment is Task task) await task;
            foreach (var fact in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(method => Attribute.IsDefined(method, typeof(FactAttribute))))
            {
                fact.Invoke(instance, null);
            }
        }
    }
}
#endif
