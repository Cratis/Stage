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

public class when_rendering_legacy_file_handlers : a_multi_slice_application
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
                slice StateChange Process
                  command ProcessBatch
                    handler
                      file Handlers/ProcessBatch.cs
                  event BatchProcessed
                  specification Processing
                    when ProcessBatch
                    then BatchProcessed
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
        _context = new([_application]);
        var command = _blocked.Commands.Single();
        command.Handler!.File!.Path.ShouldEqual("Handlers/ProcessBatch.cs");
        command.Handler.Code.ShouldBeNull();
        command.Produces.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_reject_instead_of_successfully_writing_an_empty_handle()
    {
        var error = await Catch.Exception(() => _renderer.Render([_application], _targetDirectory, _output, _error));
        Assert.True(error is RenderingFailed, $"Expected RenderingFailed, got {error?.GetType().Name ?? "success"}. Output: {_output}. Artifacts: {string.Join(Environment.NewLine, _codeOutput.Files.Select(file => file.Content))}");
        ((RenderingFailed)error!).Failures.Single().ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
        _codeOutput.Files.ShouldNotContain(file => file.Content.Contains("public void Handle()", StringComparison.Ordinal));
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
        failure.ShouldBeOfExactType<UnsupportedFileBackedCommandHandler>();
        var rejection = (UnsupportedFileBackedCommandHandler)failure;
        rejection.CommandName.ShouldEqual("ProcessBatch");
        rejection.FullSlicePath.ShouldEqual("Billing.Invoices.Process");

        // Compile(string) has no source-path argument; do not manufacture a filename for its locations.
        rejection.CommandSourceLocation.ShouldEqual(SourceLocation.Start with { Line = 4, Column = 7 });
        rejection.File.Location.ShouldEqual(SourceLocation.Start with { Line = 6, Column = 11 });
        Assert.Same(_blocked.Commands.Single().Handler!.File, rejection.File);
        rejection.File.Path.ShouldEqual("Handlers/ProcessBatch.cs");
        _error.ToString().ShouldContain(rejection.Message);
        _output.ToString().ShouldNotContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
        _blocked.Specifications.ShouldNotBeEmpty();
        _codeOutput.Files.ShouldNotContain(file => file.RelativePath.Contains("Process", StringComparison.Ordinal));
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

    [Fact]
    public async Task should_preserve_context_independent_inline_execution_with_an_unselected_file_handler()
    {
        var command = _independent.Commands.Single() with
        {
            Produces = [],
            Handler = new(null, new("csharp", "return Array.Empty<object>();", SourceLocation.Start), SourceLocation.Start),
        };
        var selected = _independent with { Commands = [command] };
        await _renderer.Render(selected, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices");
        _codeOutput.Files.Count.ShouldEqual(1);
        var assembly = RenderedOutput.Load(_codeOutput.Files);
        var commandType = assembly.GetTypes().Single(type => type.Name == "ArchiveInvoice");
        var events = (IEnumerable<object>)commandType.GetMethod("Handle")!.Invoke(Activator.CreateInstance(commandType), [null])!;
        events.ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        _error.ToString().ShouldBeEmpty();
    }
}
