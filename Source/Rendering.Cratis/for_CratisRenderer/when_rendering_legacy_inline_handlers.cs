// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_legacy_inline_handlers : a_multi_slice_application
{
    ModuleSyntax _module = null!;
    FeatureSyntax _feature = null!;
    SliceSyntax _blocked = null!;
    SliceSyntax _independent = null!;
    ApplicationSet _context = null!;

    void Establish()
    {
        const string source = """
            module Billing
              feature Invoices
                slice StateChange Register
                  command RegisterInvoice
                    produces InvoiceRegistered
                  event InvoiceRegistered
                  specification Registration
                    when RegisterInvoice
                    then InvoiceRegistered
                slice StateChange Archive
                  command ArchiveInvoice
                    produces InvoiceArchived
                  event InvoiceArchived
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        compilation.Success.ShouldBeTrue();
        _application = compilation.Value!;
        _module = _application.Modules.Single();
        _feature = _module.Features.Single();
        _blocked = _feature.Slices.First();
        _independent = _feature.Slices.Last();
        var code = new CodeBlockSyntax("csharp", "return new object[] { context.Identity.Id };", SourceLocation.Start with { Path = "legacy/Invoices.play", Line = 8, Column = 11 });
        var command = _blocked.Commands.Single() with { Handler = new(null, code, SourceLocation.Start) };
        _blocked = _blocked with { Commands = [command] };
        _feature = _feature with { Slices = [_blocked, _independent] };
        _module = _module with { Features = [_feature] };
        _application = _application with { Modules = [_module] };
        _context = new([_application]);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_fail_closed_through_each_public_entrypoint(string scope)
    {
        var error = await Catch.Exception(() => scope switch
        {
            "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
            "module" => _renderer.Render(_module, _context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(_feature, _context, _targetDirectory, _output, _error, module: "Billing"),
            _ => _renderer.Render(_blocked, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices"),
        });
        error.ShouldBeOfExactType<RenderingFailed>();
        var failure = ((RenderingFailed)error).Failures.Single();
        failure.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();
        var rejection = (UnsupportedInlineCommandHandler)failure;
        rejection.CommandName.ShouldEqual("RegisterInvoice");
        rejection.FullSlicePath.ShouldEqual("Billing.Invoices.Register");
        rejection.Location.ShouldEqual(_blocked.Commands.Single().Handler!.Code!.Location);
        rejection.Reason.ShouldEqual(InlineCommandHandlerRejectionReason.ContextBinding);
        _error.ToString().ShouldContain(rejection.Message);
        _output.ToString().ShouldNotContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
        _blocked.Specifications.ShouldNotBeEmpty();
        _codeOutput.Files.ShouldNotContain(file => file.RelativePath.Contains("Register", StringComparison.Ordinal));
        if (scope == "slice")
        {
            _codeOutput.Files.ShouldBeEmpty();
        }
        else
        {
            _codeOutput.Files.ShouldContain(file => file.Content.Contains("record ArchiveInvoice", StringComparison.Ordinal));
            RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task should_ignore_an_unsafe_unselected_slice_and_compile_the_selected_declarative_handler()
    {
        await _renderer.Render(_independent, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices");
        _codeOutput.Files.Count.ShouldEqual(1);
        _codeOutput.Files.Single().Content.ShouldContain("public InvoiceArchived Handle() => new();");
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        _error.ToString().ShouldBeEmpty();
    }
}
