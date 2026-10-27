// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_legacy_command_with_short_names : an_application_reading_the_context
{
    string _handler = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _handler = _codeOutput.Files.Single(file => file.RelativePath.EndsWith(Path.Combine("Register", "Register.cs"), StringComparison.Ordinal)).Content;
    }

    [Fact] void should_render_output_that_compiles() => RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
    [Fact] void should_shorten_unambiguous_qualified_types() => _handler.ShouldNotContain("global::");
    [Fact] void should_shorten_the_tenant_translation() => _handler.ShouldContain("PortableTenantValues.Translate(");
    [Fact] void should_import_the_tenant_translation() => _handler.ShouldContain("using AcmeBilling.GeneratedTenancy;");
    [Fact] void should_shorten_the_receipt_time() => _handler.ShouldContain("CommandReceiptTime.OccurredAtReceipt(");
    [Fact] void should_import_the_receipt_time() => _handler.ShouldContain("using AcmeBilling.GeneratedCommands;");
    [Fact] void should_shorten_the_operation_accessor() => _handler.ShouldContain("Handle(IOperationContextAccessor operation");
    [Fact] void should_resolve_the_operation_accessor_through_the_project_global_usings() =>
        FrameworkNameCatalog.Current.GlobalUsings(GeneratedProjects.Application).ShouldContain("Cratis.Arc");
    [Fact] void should_retain_the_tenant_translation_support_file() =>
        _codeOutput.Files.Any(file => file.RelativePath == Path.Combine("GeneratedTenancy", "PortableTenantValues.cs")).ShouldBeTrue();
    [Fact] void should_retain_the_receipt_time_support_file() =>
        _codeOutput.Files.Any(file => file.RelativePath == Path.Combine("GeneratedCommands", "CommandReceiptTime.cs")).ShouldBeTrue();
}
