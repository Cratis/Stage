// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_local_composite_event_signatures : Specification
{
    string _command = null!;
    string _specification = null!;
    IReadOnlyList<string> _errors = null!;
    IReadOnlyList<string> _warnings = null!;

    void Because()
    {
        var source = "type InvoiceDetails\n  description String\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource) +
            "\n      event InvoiceDetailsRecorded\n        details InvoiceDetails[]\n";
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        plan.Success.ShouldBeTrue();
        var sources = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
            .Select(_ => new RenderedFile(_.RelativePath, Encoding.UTF8.GetString(_.Bytes.AsSpan()))).ToArray();
        _command = sources.Single(_ => _.RelativePath.EndsWith("/Issue.cs", StringComparison.Ordinal)).Content;
        _specification = sources.Single(_ => _.RelativePath.EndsWith("/when_issuing_first_invoice.cs", StringComparison.Ordinal)).Content;
        _errors = RenderedOutput.Errors(sources);
        _warnings = RenderedOutput.Warnings(sources);
    }

    [Fact] void should_import_common_for_a_locally_emitted_composite_signature() => _command.ShouldContain("using Invoices.Common;");
    [Fact] void should_render_the_composite_collection_signature() => _command.ShouldContain("public record InvoiceDetailsRecorded(IReadOnlyList<InvoiceDetails> Details);");
    [Fact] void should_not_import_common_in_specs_that_emit_only_primitive_values() => _specification.ShouldNotContain("using Invoices.Common;");
    [Fact] void should_compile_the_generated_declarations_and_specifications() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, _warnings).ShouldEqual(string.Empty);
}
