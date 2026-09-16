// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_primitive_invoice_commands : Specification
{
    readonly ICollection<(ArtifactRenderPlan First, ArtifactRenderPlan Second)> _plans = [];
    readonly List<string> _errors = [];
    readonly List<string> _warnings = [];
    readonly ICollection<(string Command, string Type, string Destination, string Specification, string ExpectedSource)> _sources = [];

    void Because()
    {
        (string Type, string First, string Second, string CSharpType, string ValueExpression)[] cases =
        [
            ("String", invoice_model.TextSource, invoice_model.OtherTextSource, "string", invoice_model.TextSource),
            ("Uuid", invoice_model.UuidSource, invoice_model.OtherUuidSource, "Guid", $"Guid.Parse({invoice_model.UuidSource})"),
            ("Int", "-42", "73", "int", "-42"),
            ("Decimal", "-12.5", "73.25", "decimal", "-12.5m"),
            ("Bool", "true", "false", "bool", "true"),
            ("Date", "\"2026-01-02\"", "\"2026-02-03\"", "DateOnly", "DateOnly.Parse(\"2026-01-02\", CultureInfo.InvariantCulture)"),
            ("DateTime", "\"2026-01-02T03:04:05.0000000+00:00\"", "\"2026-02-03T04:05:06.0000000+00:00\"", "DateTimeOffset", "DateTimeOffset.Parse(\"2026-01-02T03:04:05.0000000+00:00\", CultureInfo.InvariantCulture)")
        ];
        foreach (var item in cases)
        {
            var model = invoice_model.Compile(invoice_model.Source(item.Type, item.First, item.Second));
            model.Application.Concepts.ShouldBeEmpty();
            var first = invoice_model.Plan(model);
            _plans.Add((first, invoice_model.Plan(model)));
            var files = first.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
                .Select(_ => new RenderedFile(_.RelativePath, Encoding.UTF8.GetString(_.Bytes.AsSpan()))).ToArray();
            _errors.AddRange(RenderedOutput.Errors(files));
            _warnings.AddRange(RenderedOutput.Warnings(files));
            var direct = string.Equals(item.Type, "String", StringComparison.Ordinal) || string.Equals(item.Type, "Uuid", StringComparison.Ordinal);
            _sources.Add((
                files.Single(_ => _.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal)).Content,
                item.CSharpType,
                direct ? "StreamReference" : "new EventSourceId((StreamReference).ToString())",
                files.Single(_ => _.RelativePath.EndsWith("/when_issuing_first_invoice.cs", StringComparison.Ordinal)).Content,
                direct ? item.ValueExpression : $"new EventSourceId(({item.ValueExpression}).ToString())"));
        }
    }

    [Fact] void should_admit_every_scalar_model() => _plans.All(_ => _.First.Success).ShouldBeTrue();
    [Fact] void should_keep_the_explicit_project_name() => _plans.All(_ => _.First.Artifacts.Any(artifact => artifact.RelativePath == "InvoiceApp.csproj")).ShouldBeTrue();
    [Fact] void should_keep_the_explicit_namespace() => _sources.All(_ => _.Command.Contains("namespace Invoices.Billing.Invoicing.Issue;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_primitive_command_properties() => _sources.All(_ => _.Command.Contains($"public record IssueInvoice(string Description, {_.Type} StreamReference) : ICanProvideEventSourceId", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_resolve_the_semantic_destination() => _sources.All(_ => _.Command.Contains($"public EventSourceId GetEventSourceId() => {_.Destination};", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_append_only_through_the_returned_event() => _sources.All(_ => _.Command.Contains("public InvoiceIssued Handle() => new(Description);", StringComparison.Ordinal) && !_.Command.Contains("IEventLog", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_assert_the_actual_event_source_and_payload() => _sources.All(_ => _.Specification.Contains($"ShouldHaveAppendedEvent<IssueInvoice, InvoiceIssued>({_.ExpectedSource}, @event => @event.Description == \"First payload\")", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_import_a_nonexistent_common_namespace() => _sources.All(_ => !_.Command.Contains("using Invoices.Common;", StringComparison.Ordinal) && !_.Specification.Contains("using Invoices.Common;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_create_concepts_for_primitives() => _plans.All(_ => !_.First.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_compile_generated_debug_specifications() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, _warnings).ShouldEqual(string.Empty);
    [Fact] void should_repeat_the_same_paths() => _plans.All(_ => _.First.Artifacts.Select(artifact => artifact.RelativePath).SequenceEqual(_.Second.Artifacts.Select(artifact => artifact.RelativePath))).ShouldBeTrue();
    [Fact] void should_repeat_the_same_hashes() => _plans.All(_ => _.First.Artifacts.Select(artifact => artifact.Sha256).SequenceEqual(_.Second.Artifacts.Select(artifact => artifact.Sha256))).ShouldBeTrue();
    [Fact] void should_repeat_the_same_bytes() => _plans.All(_ => _.First.Artifacts.Zip(_.Second.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes))).ShouldBeTrue();
}
