// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_command_with_conditional_produces : state_change_slices
{
    RenderedFile _file = null!;
    StateChangeSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateChangeSliceRenderer();

    void Because() => _file = _renderer.Render(_cancelInvoice, _applicationSet, "CratisApp");

    [Fact] void should_declare_handle_returning_multiple_events() => _file.Content.ShouldContain("public IEnumerable<object> Handle()");
    [Fact] void should_build_an_events_list() => _file.Content.ShouldContain("var events = new List<object>();");
    [Fact] void should_guard_the_conditional_event() => _file.Content.ShouldContain("if (Reason != \"\")");
    [Fact] void should_add_the_conditional_event() => _file.Content.ShouldContain("events.Add(new InvoiceCancelled(Reason));");
    [Fact] void should_add_the_unconditional_event() => _file.Content.ShouldContain("events.Add(new InvoiceRefundRequested());");
    [Fact] void should_return_the_events() => _file.Content.ShouldContain("return events;");
    [Fact] void should_emit_a_key_attribute_for_an_unconcepted_identifier() => _file.Content.ShouldContain("[Key] Guid InvoiceId");
    [Fact] void should_import_keys_for_the_key_attribute() => _file.Content.ShouldContain("using Cratis.Chronicle.Keys;");
}
