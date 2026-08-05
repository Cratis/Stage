// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application : a_multi_slice_application
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_scaffold_the_target_directory_first() => _scaffolder.WasCalled.ShouldBeTrue();
    [Fact] void should_write_the_concept_file() => _codeOutput.Files.Any(file => file.RelativePath.EndsWith("InvoiceId.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_write_the_state_change_slice_file() =>
        _codeOutput.Files.Any(file => file.RelativePath.EndsWith(Path.Combine("RegisterInvoice", "RegisterInvoice.cs"), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_write_the_state_view_slice_file() =>
        _codeOutput.Files.Any(file => file.RelativePath.EndsWith(Path.Combine("InvoiceSummary", "InvoiceSummary.cs"), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_progress_for_each_slice() => _output.ToString().ShouldContain("Rendering slice 'Billing.Invoices.RegisterInvoice'...");
    [Fact] void should_report_completion() => _output.ToString().ShouldContain("Rendering complete.");
    [Fact] void should_not_report_any_errors() => _error.ToString().ShouldEqual(string.Empty);
}
