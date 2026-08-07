// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_cross_slice_references : an_application_with_cross_slice_references
{
    IReadOnlyList<string> _compilationErrors = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _compilationErrors = RenderedOutput.Errors(_codeOutput.Files);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_import_the_declaring_slice_namespace_into_the_projecting_slice() =>
        SliceContent("Summary").ShouldContain("using AcmeBilling.Billing.Registration.Register;");
    [Fact] void should_import_the_declaring_slice_namespace_into_the_reacting_slice() =>
        SliceContent("Notify").ShouldContain("using AcmeBilling.Billing.Registration.Register;");
    [Fact] void should_key_the_read_model_on_the_property_the_key_is_mapped_into() =>
        SliceContent("Summary").ShouldContain("[Key] [SetFrom<InvoiceRegistered>(nameof(InvoiceRegistered.InvoiceNumber))] InvoiceNumber Number");

    string SliceContent(string slice) =>
        _codeOutput.Files.Single(file => file.RelativePath.EndsWith(Path.Combine(slice, $"{slice}.cs"), StringComparison.Ordinal)).Content;
}
