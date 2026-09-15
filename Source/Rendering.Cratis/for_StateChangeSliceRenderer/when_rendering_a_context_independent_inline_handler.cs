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
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_context_independent_inline_handler : state_change_slices
{
    const string Body = "return Array.Empty<object>();";
    RenderedFile _file = null!;
    IEnumerable<object> _events = null!;
    Type _contextType = null!;

    void Establish()
    {
        var command = _processBatch.Slice.Commands.Single();
        command = command with { Handler = command.Handler! with { Code = command.Handler.Code! with { Code = Body } } };
        var completeEvent = new EventSyntax("BatchProcessed", [new PropertySyntax("identityId", new("String", false, false, SourceLocation.Start), SourceLocation.Start)], SourceLocation.Start);
        _processBatch = _processBatch with { Slice = _processBatch.Slice with { Commands = [command], Events = [completeEvent] } };
    }

    void Because()
    {
        _file = new StateChangeSliceRenderer().Render(_processBatch, _applicationSet, "CratisApp");
        var assembly = RenderedOutput.Load([_file]);
        var commandType = assembly.GetType("CratisApp.Billing.Invoices.ProcessBatch.ProcessBatch")!;
        var command = Activator.CreateInstance(commandType, Guid.Empty);
        var handle = commandType.GetMethod("Handle")!;
        _contextType = handle.GetParameters().Single().ParameterType;
        _events = (IEnumerable<object>)handle.Invoke(command, [null])!;
    }

    [Fact] void should_emit_the_accepted_body_verbatim() => _file.Content.ShouldContain(Body);
    [Fact] void should_not_emit_the_obsolete_context_compatibility_warning() => _file.Diagnostics.ShouldBeEmpty();
    [Fact] void should_compile_against_the_real_arc_context() => _contextType.ShouldEqual(typeof(CommandContext));
    [Fact] void should_compile_without_errors() => RenderedOutput.Errors([_file]).ShouldBeEmpty();
    [Fact] void should_execute_the_generated_handle_and_return_no_events() => _events.ShouldBeEmpty();
    [Fact] void should_include_the_complete_fixture_event() => _file.Content.ShouldContain("record BatchProcessed(string IdentityId)");
}
#endif
