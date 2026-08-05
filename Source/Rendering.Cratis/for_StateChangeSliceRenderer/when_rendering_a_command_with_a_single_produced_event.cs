// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_command_with_a_single_produced_event : state_change_slices
{
    RenderedFile _file = null!;
    StateChangeSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateChangeSliceRenderer();

    void Because() => _file = _renderer.Render(_registerInvoice, _applicationSet, "CratisApp");

    [Fact] void should_place_the_file_under_its_own_slice_folder() =>
        _file.RelativePath.ShouldEqual(Path.Combine("Billing", "Invoices", "RegisterInvoice", "RegisterInvoice.cs"));
    [Fact] void should_declare_the_namespace_including_the_slice_name() =>
        _file.Content.ShouldContain("namespace CratisApp.Billing.Invoices.RegisterInvoice;");
    [Fact] void should_not_import_common_since_the_concept_is_used_only_by_this_slice() => _file.Content.ShouldNotContain("using CratisApp.Common;");
    [Fact] void should_emit_the_command_attribute() => _file.Content.ShouldContain("[Command]");
    [Fact] void should_declare_the_command_record() => _file.Content.ShouldContain("public record RegisterInvoice(InvoiceId InvoiceId, string Name)");
    [Fact] void should_not_emit_a_key_attribute_for_a_concept_typed_identifier() => _file.Content.ShouldNotContain("[Key]");
    [Fact] void should_emit_an_expression_bodied_handle_for_a_single_unconditional_event() =>
        _file.Content.ShouldContain("public InvoiceRegistered Handle() => new(Name);");
    [Fact] void should_emit_the_event_type() => _file.Content.ShouldContain("[EventType]");
    [Fact] void should_declare_the_event_record() => _file.Content.ShouldContain("public record InvoiceRegistered(string Name);");
    [Fact] void should_document_the_event_property() => _file.Content.ShouldContain("/// <param name=\"Name\">The name.</param>");
}
