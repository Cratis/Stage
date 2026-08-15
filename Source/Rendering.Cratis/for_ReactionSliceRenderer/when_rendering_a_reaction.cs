// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ReactionSliceRenderer;

public class when_rendering_a_reactor : an_automation_slice
{
    RenderedFile _file = null!;
    ReactionSliceRenderer _renderer = null!;

    void Establish() => _renderer = new ReactionSliceRenderer();

    void Because() => _file = _renderer.Render(_detectorSlice, _applicationSet, "CratisApp");

    [Fact] void should_place_the_file_under_its_own_slice_folder() =>
        _file.RelativePath.ShouldEqual(Path.Combine("Billing", "Invoices", "DetectOverdueInvoices", "DetectOverdueInvoices.cs"));
    [Fact] void should_declare_the_reactor_class() => _file.Content.ShouldContain("public class OverdueInvoiceDetector : IReactor");
    [Fact] void should_use_the_authored_description_as_summary() => _file.Content.ShouldContain("/// Detects overdue invoices");
    [Fact] void should_declare_a_method_per_trigger() => _file.Content.ShouldContain("public IEnumerable<object>? InvoiceRegistered(InvoiceRegistered @event, EventContext context)");
    [Fact] void should_deconstruct_the_triggering_event_properties() => _file.Content.ShouldContain("var (DueDate) = @event;");
    [Fact] void should_embed_the_authored_code_verbatim() => _file.Content.ShouldContain("return [new MarkInvoiceOverdue(DueDate)];");
    [Fact] void should_stub_a_file_based_trigger() => _file.Content.ShouldContain("// TODO: implementation lives in 'Reactors/NotifyCustomerReactor.cs' — not embedded in this pass");
    [Fact] void should_throw_not_implemented_for_a_file_based_trigger() => _file.Content.ShouldContain("throw new NotImplementedException();");
    [Fact] void should_stub_a_bare_trigger() => _file.Content.ShouldContain("// TODO: implement the reaction to InvoicePaid");
    [Fact] void should_return_null_for_a_bare_trigger() => _file.Content.ShouldContain("return null;");
}
