// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_validated_command_with_a_specification : a_multi_slice_application
{
    void Establish()
    {
        var compilation = new ScreenplayCompiler().Compile("""
            module Billing
              feature Invoices
                slice StateChange Register
                  command RegisterInvoice
                    name String
                    validate
                      name not empty
                    produces InvoiceRegistered
                      name = name
                  event InvoiceRegistered
                    name String
                  specification Registering
                    when RegisterInvoice
                      name = "invoice"
                    then InvoiceRegistered
                      name = "invoice"
            """);
        compilation.Success.ShouldBeTrue();
        _application = compilation.Value!;
    }

    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_the_command_validator() => _codeOutput.Files.Single(file => file.RelativePath.EndsWith("/Register.cs", StringComparison.Ordinal)).Content.ShouldContain("class RegisterInvoiceValidator : CommandValidator<RegisterInvoice>");
    [Fact] void should_emit_the_specification() => _codeOutput.Files.Single(file => file.RelativePath.EndsWith("/when_registering.cs", StringComparison.Ordinal)).Content.ShouldContain("class when_registering : Specification");
    [Fact] void should_compile_the_rendered_application_and_specification() => RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
    [Fact] void should_not_leave_a_failure_marker() => _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
}
