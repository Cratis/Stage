// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer;

public class when_rendering_specification_descriptions : given.a_slice_with_specifications
{
    const string Description = "Witnesses <registration> & lookup\r\nNext  \rLast\u0085Unicode\u2028Line\u2029End  ";
    const string Summary = "/// <summary>\n/// Witnesses &lt;registration&gt; &amp; lookup\n/// Next\n/// Last\n/// Unicode\n/// Line\n/// End\n/// </summary>\n";
    RenderedFile _appended = null!;
    RenderedFile _rejected = null!;
    RenderedFile _withoutDescription = null!;
    IReadOnlyList<string> _errors = null!;

    void Because()
    {
        var specification = Specification(
            "RegisteringAnInvoice",
            when: When("RegisterInvoice", ("invoiceId", "9c858901-8a57-4791-81fe-4c455b099bc9"), ("invoiceNumber", "INV-000123"), ("dueDate", "2026-08-13")),
            then: [Event("InvoiceRegistered", ("invoiceNumber", "INV-000123"))]);
        _withoutDescription = SpecificationRenderer.Render(specification, _command, _slice, _applicationSet, "Acme");
        _appended = SpecificationRenderer.Render(specification with { Description = Description }, _command, _slice, _applicationSet, "Acme");
        _rejected = SpecificationRenderer.Render(
            Specification(
                "RejectingAnInvoice",
                when: specification.When,
                errors: [new(null, SourceLocation.Start)]) with { Description = "Witnesses <rejection> & no events" },
            _command,
            _slice,
            _applicationSet,
            "Acme");
        var slice = new StateChangeSliceRenderer().Render(_slice, _applicationSet, "Acme");
        var concepts = _applicationSet.Concepts.Values.Select(concept => ConceptRenderer.Render(concept, _applicationSet, "Acme"));
        _errors = RenderedOutput.Errors([slice, _appended, _rejected, .. concepts]);
    }

    [Fact] void should_escape_and_normalize_the_summary_before_the_class() => _appended.Content.ShouldContain(Summary + "public class ");
    [Fact] void should_describe_the_rejected_command_scenario() => _rejected.Content.ShouldContain("/// <summary>\n/// Witnesses &lt;rejection&gt; &amp; no events\n/// </summary>\npublic class ");
    [Fact] void should_compile_the_described_debug_specifications() => _errors.ShouldBeEmpty();
    [Fact] void should_not_invent_a_summary_without_a_description() => _withoutDescription.Content.ShouldNotContain("/// <summary>");
    [Fact] void should_preserve_every_other_byte() => _appended.Content.Replace(Summary, string.Empty, StringComparison.Ordinal).ShouldEqual(_withoutDescription.Content);
}
