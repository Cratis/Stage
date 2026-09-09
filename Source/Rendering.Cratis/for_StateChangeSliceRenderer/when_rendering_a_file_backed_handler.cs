// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_file_backed_handler : a_file_handler_slice
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => new StateChangeSliceRenderer().Render(_slice, _applicationSet, "CratisApp"));

    [Fact] void should_reject_instead_of_emitting_an_empty_handle() => _error.ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
    [Fact] void should_identify_the_command() => ((UnsupportedFileBackedCommandHandler)_error).CommandName.ShouldEqual("ProcessBatch");
    [Fact] void should_identify_the_selected_slice() => ((UnsupportedFileBackedCommandHandler)_error).FullSlicePath.ShouldEqual("Billing.Invoices.Process");
    [Fact] void should_preserve_the_command_source_location() => ((UnsupportedFileBackedCommandHandler)_error).CommandSourceLocation.ShouldEqual(SourceLocation.Start with { Path = SourcePath, Line = 4, Column = 7 });
    [Fact] void should_preserve_the_file_reference() => Assert.Same(_command.Handler!.File, ((UnsupportedFileBackedCommandHandler)_error).File);
    [Fact] void should_preserve_the_symbolic_implementation_path() => ((UnsupportedFileBackedCommandHandler)_error).File.Path.ShouldEqual("Handlers/ProcessBatch.cs");
    [Fact] void should_preserve_the_file_directive_source_location() => ((UnsupportedFileBackedCommandHandler)_error).File.Location.ShouldEqual(SourceLocation.Start with { Path = SourcePath, Line = 6, Column = 11 });
    [Fact] void should_have_a_stable_unique_diagnostic_code() => UnsupportedFileBackedCommandHandler.DiagnosticCode.ShouldEqual("STAGE-CRATIS-FILE-001");
    [Fact] void should_explain_unsupported_implementation_rather_than_a_missing_file() => _error.Message.ShouldEqual("STAGE-CRATIS-FILE-001: Command 'ProcessBatch' in slice 'Billing.Invoices.Process' at legacy/Batch.play(4,7): file-backed command implementation not supported by legacy Cratis renderer. Implementation reference 'Handlers/ProcessBatch.cs' at legacy/Batch.play(6,11).");
    [Fact] void should_not_report_an_io_failure() => _error.InnerException.ShouldBeNull();
    [Fact] void should_have_no_declarative_fallback_in_the_authored_command() => _command.Produces.ShouldBeEmpty();
    [Fact] void should_have_no_inline_body_in_the_authored_command() => _command.Handler!.Code.ShouldBeNull();
}
