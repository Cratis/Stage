// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Commands;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_an_inline_handler_with_a_single_event_return : state_change_slices
{
    RenderedFile _file = null!;
    object _event = null!;
    Type _returnType = null!;
    Type _contextType = null!;

    void Establish()
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler with { Code = command.Handler.Code with { Code = "return new OrderPlaced();" } } };
        var placed = new EventSyntax("OrderPlaced", [], SourceLocation.Start);
        _processBatch = _processBatch with { Slice = _processBatch.Slice with { Commands = [command], Events = [placed] } };
    }

    void Because()
    {
        _file = new StateChangeSliceRenderer().Render(_processBatch, _applicationSet, "CratisApp");
        var assembly = RenderedOutput.Load([_file]);
        var commandType = assembly.GetType("CratisApp.Billing.Invoices.ProcessBatch.ProcessBatch");
        var handle = commandType.GetMethod("Handle");
        _returnType = handle.ReturnType;
        _contextType = handle.GetParameters().Single().ParameterType;
        _event = handle.Invoke(Activator.CreateInstance(commandType, Guid.Empty), [null]);
    }

    [Fact] void should_emit_the_explicit_event_return_type() => _file.Content.ShouldContain("public OrderPlaced Handle(CommandContext context)");
    [Fact] void should_emit_the_authored_body() => _file.Content.ShouldContain("return new OrderPlaced();");
    [Fact] void should_compile_against_the_real_arc_context() => _contextType.ShouldEqual(typeof(CommandContext));
    [Fact] void should_execute_the_handler_and_return_the_event() => _event.GetType().ShouldEqual(_returnType);
    [Fact] void should_return_the_authored_event_type() => _returnType.Name.ShouldEqual("OrderPlaced");
    [Fact] void should_not_report_rendering_diagnostics() => _file.Diagnostics.WithoutAuthenticatedDefaultNotes().ShouldBeEmpty();
}
#endif
